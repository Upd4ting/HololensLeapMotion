using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

using Leap;

using MiniJSON;

using UnityEngine;
#if !UNITY_EDITOR
using Windows.Networking.Sockets;
using Windows.Networking;

#endif

public class LeapWebProcessor : MonoBehaviour {
    // Struct for binary protocol with the hololens
    public struct HandData {
        public List<object> armBasis;
        public double       armWidth;
        public double       confidence;
        public List<object> direction;
        public List<object> elbox;
        public double       grapAngle;
        public double       grapStrength;
        public long         id;
        public List<object> palmNormal;
        public List<object> palmPosition;
        public List<object> palmVelocity;
        public double       palmWidth;
        public double       pinchStrength;
        public double       s;
        public List<object> sphereCenter;
        public double       sphereRadius;
        public List<object> stabilizedPalmPosition;
        public double       timeVisible;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 10)]
        public string type;

        public List<object> wrist;
    }

    public struct FingerData {
        public List<object> btipPosition;
        public List<object> carpPosition;
        public List<object> dipPosition;
        public List<object> direction;
        public bool         extended;
        public long         handId;
        public long         id;
        public double       length;
        public List<object> mcpPosition;
        public List<object> pipPosition;
        public List<object> stabilizedTipPosition;
        public double       timeVisible;
        public List<object> tipPosition;
        public List<object> tipVelocity;
        public bool         tool;
        public double       touchDistance;
        public long         type;
        public double       width;
        public List<object> boneBasics;
    }

    public struct FrameData {
        public long             id;
        public long             timestamp;
        public double           currentFrameRate;
        public List<object>     interactionBoxCenter;
        public List<object>     interactionBoxSize;
        public List<HandData>   hands;
        public List<FingerData> fingers;
    }

    public bool running;
#if !UNITY_EDITOR
    public StreamSocket socket;
#else
    public TcpClient socket;
#endif
    public BinaryReader reader;
    public BinaryWriter writer;

    public Frame frame;

    public bool IsConnected { get; internal set; }

    public         GameObject LeapHandController;
    public         bool       hasHand;
    private static long       maxTimeStamp = -1;

    private async void Start() {
        // Getting the leap hand controller hand object and set it to not active unless the program is receiving 
        // frame data from the leap motion server. This avoiding the exception that will be thrown if the frame
        // is null when the controller tried to update the frame data.
        LeapHandController = transform.GetChild(0).gameObject;
        LeapHandController.SetActive(false);
        transform.localPosition = new Vector3(-0.019f, 0.083f, 0.1f);
        IsConnected             = false;
        running                 = false;

        foreach (Transform child in GameObject.Find("HandsManager").transform) child.gameObject.SetActive(true);

        await InitSocket("192.168.1.39"); // Change the IP here to use your server or provide a way to type it
    }

    private void Update() {
        if (IsConnected && !LeapHandController.activeSelf) LeapHandController.SetActive(true);
    }

    public async Task InitSocket(string ipAddress) {
    #if !UNITY_EDITOR
        socket = new StreamSocket();
        await socket.ConnectAsync(new HostName(ipAddress), "4269");
        reader = new BinaryReader(socket.InputStream.AsStreamForRead());
        writer = new BinaryWriter(socket.OutputStream.AsStreamForWrite());
    #else
        socket = new TcpClient();
        await socket.ConnectAsync(ipAddress, 4269);
        reader = new BinaryReader(socket.GetStream());
        writer = new BinaryWriter(socket.GetStream());
    #endif
        running     = true;
        IsConnected = true;
        writer.Write(20); // We request 20 fps here
        writer.Flush();

        StartReceivingThread();
    }

    private void StartReceivingThread() {
        Task.Run(() => {
            while (running)
                try {
                    int size = reader.ReadInt32();
                    byte[] dataArray = reader.ReadBytes(size);
                    FrameData data = BytesToData(dataArray);
                    Debug.Log("Received frame data");

                    Frame newFrame = createFrame(data);
                    if (newFrame.Timestamp > maxTimeStamp) {
                        frame        = newFrame;
                        maxTimeStamp = newFrame.Timestamp;
                    }

                    if (IsConnected != true) IsConnected = true;
                } catch {
                    // Swallow close exception
                }
        });
    }

    private Frame createFrame(FrameData data) {
        Vector center = creatVector(data.interactionBoxCenter);
        Vector size = creatVector(data.interactionBoxSize);
        InteractionBox interactionBox = new InteractionBox(center, size);

        //TODO next

        //HandObject
        List<object> handsobject = (List<object>) frameData["hands"];
        List<Hand>   hands       = new List<Hand>();

        //Creating the hands objects. 
        List<object> pointables = (List<object>) frameData["pointables"];
        if (handsobject.Count == 1) {
            //If there is one hand
            hasHand = true;
            hands.Add(creatHand(id, handsobject[0], pointables));
        } else if (handsobject.Count == 2) {
            // If there are two hands
            hasHand = true;
            hands.Add(creatHand(id, handsobject[0], pointables.GetRange(0, 5)));
            hands.Add(creatHand(id, handsobject[1], pointables.GetRange(5, 5)));
        } else {
            hasHand = false;
        }

        return new Frame(id, timestamp, (float) fps, interactionBox, hands);
    }

    private Hand creatHand(long frameId, object hand, object pointables_object) {
        bool                       isLeft          = false;
        Dictionary<string, object> handsobject     = (Dictionary<string, object>) hand;
        List<object>               pointables_list = (List<object>) pointables_object;
        List<object>               armbasislist    = (List<object>) handsobject["armBasis"];
        Vector                     arm_1           = creatVector((List<object>) armbasislist[0]);
        Vector                     arm_2           = creatVector((List<object>) armbasislist[1]);
        Vector                     arm_3           = creatVector((List<object>) armbasislist[2]);

        double armWidth = (double) handsobject["armWidth"];

        double confidence = (double) handsobject["confidence"];

        List<object> direction_list = (List<object>) handsobject["direction"];
        Vector       direction      = creatVector(direction_list);

        List<object> elbow_list = (List<object>) handsobject["elbow"];
        Vector       elbow      = creatVector(elbow_list);

        double grabStrength = (double) handsobject["grabStrength"];

        long Handid = (long) handsobject["id"];

        List<object> palmNormal_list = (List<object>) handsobject["palmNormal"];
        Vector       palmNormal      = creatVector(palmNormal_list);

        List<object> palmPosition_list = (List<object>) handsobject["palmPosition"];
        Vector       palmPosition      = creatVector(palmPosition_list);

        List<object> palmVelocity_list = (List<object>) handsobject["palmVelocity"];
        Vector       palmVelocity      = creatVector(palmVelocity_list);

        double pinchStrength = (double) handsobject["pinchStrength"];

        double s = (double) handsobject["s"];

        List<object> sphereCenter_list = (List<object>) handsobject["sphereCenter"];
        Vector       sphereCenter      = creatVector(sphereCenter_list);

        double sphereRadius = (double) handsobject["sphereRadius"];

        List<object> stabilizedPalmPosition_list = (List<object>) handsobject["stabilizedPalmPosition"];
        Vector       stabilizedPalmPosition      = creatVector(stabilizedPalmPosition_list);

        double timeVisible = (double) handsobject["timeVisible"];

        string type = (string) handsobject["type"];
        if (type.Equals("left"))
            isLeft = true;
        else
            isLeft = false;

        List<object> wrist_list = (List<object>) handsobject["wrist"];
        Vector       wrist      = creatVector(wrist_list);
        Vector       midpoint   = new Vector((elbow.x + wrist.x) / 2, (elbow.y + wrist.y) / 2, (elbow.z + wrist.z) / 2);

        LeapQuaternion basis = createQuaternion(arm_1, arm_2, arm_3);

        // Note that the armlength and some other data requried form the arm constructor is replaced by a filler 245
        // I couldn't quite how to figure out how to get those data from the leap motion data. I notiecd that the length
        // is usually around 245(which is my hand), so I replaced it with the data.
        Arm arm = new Arm(elbow, wrist, midpoint, direction, 245, (float) armWidth, basis);

        List<Finger> fingerlist = createFingerlist(Handid, frameId, pointables_list);

        //Same with the arm object, I couldn't figure out the grablength from the leap motion. This may be causing some of the perfromace issues that I have with
        // the hand model in real time.
        Hand result = new Hand(frameId, (int) Handid, (float) confidence, (float) grabStrength, 0, (float) pinchStrength, 0, (float) s, isLeft, (float) timeVisible, arm, fingerlist, palmPosition,
                               stabilizedPalmPosition, palmVelocity, palmNormal, LeapQuaternion.Identity, direction, wrist);

        return result;
    }

    //Create bone objects and fingers from the leap motion pointalbes data.
    private List<Finger> createFingerlist(long Handid, long frameId, List<object> pointables_list) {
        List<Finger> result = new List<Finger>();
        foreach (object item in pointables_list) {
            Dictionary<string, object> pointables = (Dictionary<string, object>) item;

            List<object> btipPosition_list = (List<object>) pointables["btipPosition"];
            Vector       btipPosition      = creatVector(btipPosition_list);

            List<object> carpPosition_list = (List<object>) pointables["carpPosition"];
            Vector       carpPosition      = creatVector(carpPosition_list);

            List<object> dipPosition_list = (List<object>) pointables["dipPosition"];
            Vector       dipPosition      = creatVector(dipPosition_list);

            List<object> direction_list = (List<object>) pointables["direction"];
            Vector       direction      = creatVector(direction_list);

            bool extended = (bool) pointables["extended"];

            long handId = (long) pointables["handId"];

            long id = (long) pointables["id"];

            double length = (double) pointables["length"];

            List<object> mcpPosition_list = (List<object>) pointables["mcpPosition"];
            Vector       mcpPosition      = creatVector(mcpPosition_list);

            List<object> pipPosition_list = (List<object>) pointables["pipPosition"];
            Vector       pipPosition      = creatVector(pipPosition_list);

            List<object> stabilizedTipPosition_list = (List<object>) pointables["stabilizedTipPosition"];
            Vector       stabilizedTipPosition      = creatVector(stabilizedTipPosition_list);

            double timeVisible = (double) pointables["timeVisible"];

            List<object> tipPosition_list = (List<object>) pointables["tipPosition"];
            Vector       tipPosition      = creatVector(tipPosition_list);

            List<object> tipVelocity_list = (List<object>) pointables["tipVelocity"];
            Vector       tipVelocity      = creatVector(tipVelocity_list);

            bool tool = (bool) pointables["tool"];

            double touchDistance = (double) pointables["touchDistance"];

            long              type_index = (long) pointables["type"];
            Finger.FingerType type;
            switch (type_index) {
                case 0:
                    type = Finger.FingerType.TYPE_THUMB;
                    break;
                case 1:
                    type = Finger.FingerType.TYPE_INDEX;
                    break;
                case 2:
                    type = Finger.FingerType.TYPE_MIDDLE;
                    break;
                case 3:
                    type = Finger.FingerType.TYPE_RING;
                    break;
                case 4:
                    type = Finger.FingerType.TYPE_PINKY;
                    break;

                default:
                    type = Finger.FingerType.TYPE_UNKNOWN;
                    break;
            }

            double width = (double) pointables["width"];

            float fingerId = handId + type_index; // Why calculating that? We have id that already is the id of the finger..

            List<object> boneBasis  = (List<object>) pointables["bases"];
            List<object> bonelist_1 = (List<object>) boneBasis[0];
            List<object> bonelist_2 = (List<object>) boneBasis[1];
            List<object> bonelist_3 = (List<object>) boneBasis[2];
            List<object> bonelist_4 = (List<object>) boneBasis[3];

            Bone metacarpal   = createBone(bonelist_1, carpPosition, mcpPosition,  (float) width, Bone.BoneType.TYPE_METACARPAL);
            Bone proximal     = createBone(bonelist_2, mcpPosition,  pipPosition,  (float) width, Bone.BoneType.TYPE_PROXIMAL);
            Bone intermediate = createBone(bonelist_3, pipPosition,  dipPosition,  (float) width, Bone.BoneType.TYPE_INTERMEDIATE);
            Bone distal       = createBone(bonelist_4, dipPosition,  btipPosition, (float) width, Bone.BoneType.TYPE_DISTAL);

            //TODO PROBLEM HERE with id passing
            Finger finger = new Finger(id, (int) frameId, (int) fingerId, (float) timeVisible, tipPosition, tipVelocity, direction, stabilizedTipPosition, (float) width, (float) length, extended,
                                       type, metacarpal, proximal, intermediate, distal);
            result.Add(finger);
        }

        return result;
    }

    private Bone createBone(List<object> basis, Vector start, Vector end, float width, Bone.BoneType type) {
        Vector basis_1   = creatVector((List<object>) basis[0]);
        Vector basis_2   = creatVector((List<object>) basis[1]);
        Vector basis_3   = creatVector((List<object>) basis[2]);
        Vector center    = new Vector((end.x + start.x) / 2,                                                                  (start.y + end.y) / 2, (start.z + end.z) / 2);
        Vector direction = new Vector(end.x                                                                        - start.x, end.y - start.y,       end.z - start.z);
        double length    = Math.Sqrt((end.x - start.x) * (end.x - start.x) + (end.y - start.y) * (end.y - start.y) + (end.z - start.z) * (end.z - start.z));

        //LeapQuaternion orientation = createQuaternion(basis_1, basis_2, basis_3);
        LeapQuaternion orientation = createQuaternion(basis_1, basis_2, basis_3);
        Bone           bone        = new Bone(start, end, center, direction, (float) length, width, type, orientation);

        return bone;
    }

    private LeapQuaternion createQuaternion(Vector arm_1, Vector arm_2, Vector arm_3) {
        Vector3        arm1_3 = new Vector3(arm_1.x, arm_1.y, arm_1.z);
        Vector3        arm2_3 = new Vector3(arm_2.x, arm_2.y, arm_2.z);
        Vector3        arm3_3 = new Vector3(arm_3.x, arm_3.y, arm_3.z);
        Quaternion     basisQ = Quaternion.LookRotation(arm3_3, arm2_3);
        LeapQuaternion basis  = new LeapQuaternion(basisQ.x, basisQ.y, basisQ.z, basisQ.w);
        return basis;
    }

    private Vector creatVector(List<object> List) {
        double x = (double) List[0];
        double y = (double) List[1];
        double z = (double) List[2];
        return new Vector((float) x, (float) y, (float) z);
    }

    private Vector4 creatVector4(List<object> List) {
        double x = (double) List[0];
        double y = (double) List[1];
        double z = (double) List[2];
        double w = (double) List[3];
        return new Vector4((float) x, (float) y, (float) z, (float) w);
    }

    private byte[] DataToBytes(FrameData str) {
        int    size = Marshal.SizeOf(str);
        byte[] arr  = new byte[size];

        IntPtr ptr = Marshal.AllocHGlobal(size);
        Marshal.StructureToPtr(str, ptr, true);
        Marshal.Copy(ptr, arr, 0, size);
        Marshal.FreeHGlobal(ptr);
        return arr;
    }

    private FrameData BytesToData(byte[] arr) {
        FrameData str = new FrameData();

        int    size = Marshal.SizeOf(str);
        IntPtr ptr  = Marshal.AllocHGlobal(size);

        Marshal.Copy(arr, 0, ptr, size);

        str = Marshal.PtrToStructure<FrameData>(ptr);
        Marshal.FreeHGlobal(ptr);

        return str;
    }

    public void StopConnection() {
        if (!IsConnected)
            return;

        running     = false;
        IsConnected = false;

        writer.Write(-1); // We tell him to disconnect
        writer.Flush();
    }

    public void StartConnection() {
        if (!IsConnected) Start();
    }
}