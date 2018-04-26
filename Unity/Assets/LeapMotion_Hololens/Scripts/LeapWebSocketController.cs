
using UnityEngine;
using System;
using Leap;


namespace LeapWrapper
{
    public class LeapWebSocketController : MonoBehaviour, IController
    {
        public LeapWebProcessor processor;
        public DeviceList Devices { get; internal set; }
        public event EventHandler<DeviceEventArgs> Device;
        public System.Func<object> Connect { get; internal set; }
        public Action<object, LeapEventArgs> DistortionChange { get; internal set; }
        public event EventHandler<ConnectionLostEventArgs> Disconnect;
        public event EventHandler<FrameEventArgs> FrameReady;
        public event EventHandler<DeviceEventArgs> DeviceLost;
        public event EventHandler<ImageEventArgs> ImageReady;
        public event EventHandler<DeviceFailureEventArgs> DeviceFailure;
        public event EventHandler<LogEventArgs> LogMessage;
        public event EventHandler<PolicyEventArgs> PolicyChange;    
        public event EventHandler<ConfigChangeEventArgs> ConfigChange;

        void Start()
        {

        }

        void OnDestroy()
        {

            StopConnection();
        }

        public Config Config
        {
            get
            {
                return null;
            }
        }

        public bool IsConnected
        {
            get
            {
                return processor.IsConnected;
            }
        }

        bool IController.IsConnected => (processor.IsConnected);//throw new NotImplementedException();

        Config IController.Config => (null);//throw new NotImplementedException();

        DeviceList IController.Devices => (null);//throw new NotImplementedException();

        event EventHandler<ConnectionEventArgs> IController.Connect
        {
            add
            {
                throw new NotImplementedException();
            }

            remove
            {
                throw new NotImplementedException();
            }
        }

        event EventHandler<DeviceEventArgs> IController.Device
        {
            add
            {
                throw new NotImplementedException();
            }

            remove
            {
                throw new NotImplementedException();
            }
        }

        event EventHandler<DistortionEventArgs> IController.DistortionChange
        {
            add
            {
                throw new NotImplementedException();
            }

            remove
            {
                throw new NotImplementedException();
            }
        }

        event EventHandler<ConnectionLostEventArgs> IController.Disconnect
        {
            add
            {
                throw new NotImplementedException();
            }

            remove
            {
                throw new NotImplementedException();
            }
        }

        event EventHandler<FrameEventArgs> IController.FrameReady
        {
            add
            {
                throw new NotImplementedException();
            }

            remove
            {
                throw new NotImplementedException();
            }
        }

        event EventHandler<DeviceEventArgs> IController.DeviceLost
        {
            add
            {
                throw new NotImplementedException();
            }

            remove
            {
                throw new NotImplementedException();
            }
        }

        event EventHandler<DeviceFailureEventArgs> IController.DeviceFailure
        {
            add
            {
                throw new NotImplementedException();
            }

            remove
            {
                throw new NotImplementedException();
            }
        }

        event EventHandler<LogEventArgs> IController.LogMessage
        {
            add
            {
                throw new NotImplementedException();
            }

            remove
            {
                throw new NotImplementedException();
            }
        }

        event EventHandler<PolicyEventArgs> IController.PolicyChange
        {
            add
            {
                throw new NotImplementedException();
            }

            remove
            {
                throw new NotImplementedException();
            }
        }

        event EventHandler<ConfigChangeEventArgs> IController.ConfigChange
        {
            add
            {
                throw new NotImplementedException();
            }

            remove
            {
                throw new NotImplementedException();
            }
        }

        event EventHandler<ImageEventArgs> IController.ImageReady
        {
            add
            {
                throw new NotImplementedException();
            }

            remove
            {
                throw new NotImplementedException();
            }
        }

        event EventHandler<PointMappingChangeEventArgs> IController.PointMappingChange
        {
            add
            {
                throw new NotImplementedException();
            }

            remove
            {
                throw new NotImplementedException();
            }
        }

        event EventHandler<HeadPoseEventArgs> IController.headPoseChange
        {
            add
            {
                throw new NotImplementedException();
            }

            remove
            {
                throw new NotImplementedException();
            }
        }

        public Frame Frame(int history = 0)
        {
            return processor.frame;
        }

        public Frame GetTransformedFrame(LeapTransform trs, int history = 0)
        {
            return processor.frame.TransformedCopy(trs);
        }

        public Frame GetInterpolatedFrame(long time)
        {
            return processor.frame;
        }

        public bool IsPolicySet(Controller.PolicyFlag policy)
        {
            Debug.Log("SetPolicy: IsPolicySet");
            return true;
        }

        public long Now()
        {
            return processor.timestamp;
        }

        public void Dispose()
        {
            return;
        }

        internal void StopConnection()
        {
            processor.StopConnection();
        }

        internal void StartConnection()
        {
            processor.StartConnection();
        }

        Frame IController.Frame(int history)
        {
            throw new NotImplementedException();
        }

        Frame IController.GetTransformedFrame(LeapTransform trs, int history)
        {
            throw new NotImplementedException();
        }

        Frame IController.GetInterpolatedFrame(long time)
        {
            throw new NotImplementedException();
        }

        void IController.SetPolicy(Controller.PolicyFlag policy)
        {
            throw new NotImplementedException();
        }

        void IController.ClearPolicy(Controller.PolicyFlag policy)
        {
            throw new NotImplementedException();
        }

        bool IController.IsPolicySet(Controller.PolicyFlag policy)
        {
            throw new NotImplementedException();
        }

        long IController.Now()
        {
            throw new NotImplementedException();
        }

        void IDisposable.Dispose()
        {
            throw new NotImplementedException();
        }
    }
}


