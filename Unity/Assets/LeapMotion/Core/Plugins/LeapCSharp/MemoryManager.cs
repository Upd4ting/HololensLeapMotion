/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and confidential.                                  *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace LeapInternal {

  public delegate IntPtr Allocate(UInt32 size, eLeapAllocatorType typeHint);
  public delegate void Deallocate(IntPtr buffer);

  public class MemoryManager {

    /// <summary>
    /// Specifies whether or not a pooling strategy should be used for the
    /// internal MemoryManager.  If enabled, memory will be periodically 
    /// recycled to be used again instead of being deallocated.  
    /// 
    /// An object may be reclaimed from the pool at any time on the 
    /// worker thread.  If you are running into issues where an object
    /// you are working with is being overwritten, consider making a copy,
    /// or turning up the MinPoolSize.
    /// </summary>
    public static bool EnablePooling = false;

    /// <summary>
    /// Specifies how many objects of a specific type need to be in the pool
    /// before they will start to be recycled.  Turning this number up can
    /// help prevent issues where objects you are working with are being
    /// overwritten with new objects.  Turning this number down can reduce
    /// the total memory footprint used by the memory manager.
    /// </summary>
    public static uint MinPoolSize = 8;

    private Dictionary<IntPtr, ActiveMemoryInfo> _activeMemory = new Dictionary<IntPtr, ActiveMemoryInfo>();
    private Dictionary<PoolKey, Queue<object>> _pooledMemory = new Dictionary<PoolKey, Queue<object>>();

    ~MemoryManager() {
      foreach (var info in _activeMemory.Values) {
        info.handle.Free();
      }
      _activeMemory.Clear();
      _activeMemory = null;

      foreach (var pool in _pooledMemory.Values) {
        pool.Clear();
      }
      _pooledMemory.Clear();
      _pooledMemory = null;
    }

    public IntPtr Pin(UInt32 size, eLeapAllocatorType typeHint) {
      try {
        //Construct a key to identify the desired allocation
        PoolKey key = new PoolKey() {
          type = typeHint,
          size = size
        };

        //Attempt to find the pool that holds this type of allocation
        Queue<object> pool;
        if (!_pooledMemory.TryGetValue(key, out pool)) {
          //Construct a new pool if none exists yet
          pool = new Queue<object>();
          _pooledMemory[key] = pool;
        }

        //Attempt to get an object from the pool
        object memory;
        if (EnablePooling && pool.Count > MinPoolSize) {
          memory = pool.Dequeue();
        } else {
          //If the pool is empty, we need to construct a new object
          switch (typeHint) {
            default:
            case eLeapAllocatorType.eLeapAllocatorType_Uint8:
              memory = new byte[size];
              break;
            case eLeapAllocatorType.eLeapAllocatorType_Float:
              memory = new float[(size + sizeof(float) - 1) / sizeof(float)];
              break;
          }
        }

        //Pin the object so its address will not change
        GCHandle handle = GCHandle.Alloc(memory, GCHandleType.Pinned);
        IntPtr ptr = handle.AddrOfPinnedObject();

        //Put the information about the newly pinned allocation into the
        //active memory map so it can be retrieved and freed layer.
        _activeMemory.Add(ptr, new ActiveMemoryInfo() {
          handle = handle,
          key = key
        });

        return ptr;
      } catch (Exception) { }

      return IntPtr.Zero;
    }

    public void Unpin(IntPtr ptr) {
      try {
        //Grab the info for the given pointer
        ActiveMemoryInfo info = _activeMemory[ptr];

        //First we return the object back to its pool
        _pooledMemory[info.key].Enqueue(info.handle.Target);

        //Then we remove the pointer from the active memory map
        _activeMemory.Remove(ptr);

        //Finally we unpin the memory
        info.handle.Free();
      } catch (Exception) { }
    }

    public object GetPinnedObject(IntPtr ptr) {
      try {
        return _activeMemory[ptr].handle.Target;
      } catch (Exception) { }
      return null;
    }

    private struct PoolKey : IEquatable<PoolKey> {
      public eLeapAllocatorType type;
      public UInt32 size;

      public override int GetHashCode() {
        return (int)type | (int)size << 4;
      }

      public bool Equals(PoolKey other) {
        return type == other.type &&
               size == other.size;
      }

      public override bool Equals(object obj) {
        if (obj is PoolKey) {
          return Equals((PoolKey)obj);
        } else {
          return false;
        }
      }
    }

    private struct ActiveMemoryInfo {
      public GCHandle handle;
      public PoolKey key;
    }
  }
}
