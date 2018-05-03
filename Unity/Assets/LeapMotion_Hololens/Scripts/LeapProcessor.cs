using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

using Leap;

using MiniJSON;

using UnityEngine;
#if !UNITY_EDITOR
using Windows.Networking.Sockets;
using Windows.Networking;
using System.Runtime.Serialization;
#else
using System.Net.Sockets;

#endif

public class LeapProcessor : MonoBehaviour {
    // Struct for binary protocol with the hololens

    [Serializable]
    public class HandData {
        public List<object> armBasis;
        public double       armWidth;
        public double       confidence;
        public List<object> direction;
        public List<object> elbow;
        public double       grapAngle;
        public double       grapStrength;
        public long         id;
        public List<object> palmNormal;
        public List<object> palmPosition;
        public List<object> palmVelocity;
        public double       palmWidth;
        public double       pinchDistance;
        public double       pinchStrength;
        public double       s;
        public List<object> sphereCenter;
        public double       sphereRadius;
        public List<object> stabilizedPalmPosition;
        public double       timeVisible;

        public string type;

        public List<object> wrist;
    }

    [Serializable]
    public class FingerData {
        public List<object> bases;
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
    }

    [Serializable]
    public class FrameData {
        public double           currentFrameRate;
        public List<FingerData> fingers;
        public List<HandData>   hands;
        public long             id;
        public List<object>     interactionBoxCenter;
        public List<object>     interactionBoxSize;
        public long             timestamp;
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
    public float timestamp;

    public bool IsConnected { get; internal set; }

    public GameObject LeapHandController;
    public bool       hasHand;
    public long       maxTimeStamp = -1;

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

        await InitSocket("192.168.1.100"); // Change the IP here to use your server or provide a way to type it
    }

    private void Update() {
        if (IsConnected && !LeapHandController.activeSelf) LeapHandController.SetActive(true);
    }

    private void OnApplicationQuit() {
        StopConnection();
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
                    string    str  = reader.ReadString();
                    FrameData data = ConstructFrameData(str);

                    Debug.Log("Received frame data");

                    Frame newFrame = createFrame(data);
                    if (newFrame.Timestamp > maxTimeStamp) {
                        frame        = newFrame;
                        maxTimeStamp = newFrame.Timestamp;
                    }

                    if (IsConnected != true) IsConnected = true;
                } catch (Exception e) {
                    Debug.Log(e.Message);
                    // Swallow close exception
                }
        });
    }

    private Frame createFrame(FrameData data) {
        Vector         center         = creatVector(data.interactionBoxCenter);
        Vector         size           = creatVector(data.interactionBoxSize);
        InteractionBox interactionBox = new InteractionBox(center, size);
        List<Hand>     hands          = new List<Hand>();

        if (data.hands.Count == 1 || data.hands.Count == 2) {
            hasHand = true;
            foreach (HandData hData in data.hands) hands.Add(CreateHand(data.id, hData, data.fingers));
        } else {
            hasHand = false;
        }

        return new Frame(data.id, data.timestamp, (float) data.currentFrameRate, interactionBox, hands);
    }

    private Hand CreateHand(long frameId, HandData hand, List<FingerData> fingers) {
        bool   isLeft = false;
        Vector arm_1  = creatVector((List<object>) hand.armBasis[0]);
        Vector arm_2  = creatVector((List<object>) hand.armBasis[1]);
        Vector arm_3  = creatVector((List<object>) hand.armBasis[2]);

        Vector direction = creatVector(hand.direction);
        Vector elbow     = creatVector(hand.elbow);

        Vector palmNormal   = creatVector(hand.palmNormal);
        Vector palmPosition = creatVector(hand.palmPosition);
        Vector palmVelocity = creatVector(hand.palmVelocity);

        Vector sphereCenter           = creatVector(hand.sphereCenter);
        Vector stabilizedPalmPosition = creatVector(hand.stabilizedPalmPosition);

        if (hand.type.Equals("left"))
            isLeft = true;
        else
            isLeft = false;

        Vector wrist    = creatVector(hand.wrist);
        Vector midpoint = new Vector((elbow.x + wrist.x) / 2, (elbow.y + wrist.y) / 2, (elbow.z + wrist.z) / 2);

        LeapQuaternion basis = createQuaternion(arm_1, arm_2, arm_3);

        // Note that the armlength and some other data requried form the arm constructor is replaced by a filler 245
        // I couldn't quite how to figure out how to get those data from the leap motion data. I notiecd that the length
        // is usually around 245(which is my hand), so I replaced it with the data.
        Arm arm = new Arm(elbow, wrist, midpoint, direction, 245, (float) hand.armWidth, basis);

        List<Finger> fingerlist = createFingerlist(hand.id, frameId, fingers);

        Hand result = new Hand(frameId, (int) hand.id, (float) hand.confidence, (float) hand.grapStrength, (float) hand.grapAngle, (float) hand.pinchStrength, (float) hand.pinchDistance,
                               (float) hand.s, isLeft, (float) hand.timeVisible, arm, fingerlist, palmPosition,
                               stabilizedPalmPosition, palmVelocity, palmNormal, LeapQuaternion.Identity, direction, wrist);

        return result;
    }

    private List<Finger> createFingerlist(long handId, long frameId, List<FingerData> fingers) {
        List<Finger> result = new List<Finger>();

        foreach (FingerData finger in fingers) {
            if (finger.handId != handId)
                continue;

            Vector btipPosition          = creatVector(finger.btipPosition);
            Vector carpPosition          = creatVector(finger.carpPosition);
            Vector dipPosition           = creatVector(finger.dipPosition);
            Vector direction             = creatVector(finger.direction);
            Vector mcpPosition           = creatVector(finger.mcpPosition);
            Vector pipPosition           = creatVector(finger.pipPosition);
            Vector stabilizedTipPosition = creatVector(finger.stabilizedTipPosition);
            Vector tipPosition           = creatVector(finger.tipPosition);
            Vector tipVelocity           = creatVector(finger.tipVelocity);

            Finger.FingerType type;
            switch (finger.type) {
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

            List<object> bonelist_1 = (List<object>) finger.bases[0];
            List<object> bonelist_2 = (List<object>) finger.bases[1];
            List<object> bonelist_3 = (List<object>) finger.bases[2];
            List<object> bonelist_4 = (List<object>) finger.bases[3];

            Bone metacarpal   = createBone(bonelist_1, carpPosition, mcpPosition,  (float) finger.width, Bone.BoneType.TYPE_METACARPAL);
            Bone proximal     = createBone(bonelist_2, mcpPosition,  pipPosition,  (float) finger.width, Bone.BoneType.TYPE_PROXIMAL);
            Bone intermediate = createBone(bonelist_3, pipPosition,  dipPosition,  (float) finger.width, Bone.BoneType.TYPE_INTERMEDIATE);
            Bone distal       = createBone(bonelist_4, dipPosition,  btipPosition, (float) finger.width, Bone.BoneType.TYPE_DISTAL);

            result.Add(new Finger((int) frameId, (int) handId, (int) finger.id, (float) finger.timeVisible, tipPosition, tipVelocity, direction, stabilizedTipPosition, (float) finger.width,
                                  (float) finger.length, finger.extended,
                                  type, metacarpal, proximal, intermediate, distal));
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

    private FrameData ConstructFrameData(string res) {
        FrameData frame = new FrameData();

        Dictionary<string, object> frameData = (Dictionary<string, object>) Json.Deserialize(res);

        if (!frameData.ContainsKey("id"))
            return null;

        frame.id               = (long) frameData["id"];
        frame.timestamp        = (long) frameData["timestamp"];
        frame.currentFrameRate = (double) frameData["currentFrameRate"];

        Dictionary<string, object> box = (Dictionary<string, object>) frameData["interactionBox"];
        frame.interactionBoxCenter = (List<object>) box["center"];
        frame.interactionBoxSize   = (List<object>) box["size"];

        List<object>     handsobject = (List<object>) frameData["hands"];
        List<object>     pointables  = (List<object>) frameData["pointables"];
        List<HandData>   hands       = new List<HandData>();
        List<FingerData> fingers     = new List<FingerData>();

        foreach (object o in handsobject) hands.Add(ConstructHandData(o));

        foreach (object o in pointables) fingers.Add(ConstructFingerData(o));

        frame.hands   = hands;
        frame.fingers = fingers;

        return frame;
    }

    private HandData ConstructHandData(object h) {
        HandData                   hand = new HandData();
        Dictionary<string, object> ho   = (Dictionary<string, object>) h;

        hand.armBasis               = (List<object>) ho["armBasis"];
        hand.armWidth               = (double) ho["armWidth"];
        hand.confidence             = (double) ho["confidence"];
        hand.direction              = (List<object>) ho["direction"];
        hand.elbow                  = (List<object>) ho["elbow"];
        hand.grapAngle              = ho.ContainsKey("grapAngle") ? (double) ho["grapAngle"] : 0d;
        hand.grapStrength           = ho.ContainsKey("grapStrength") ? (double) ho["grapStrength"] : 0d;
        hand.id                     = (long) ho["id"];
        hand.palmNormal             = (List<object>) ho["palmNormal"];
        hand.palmPosition           = (List<object>) ho["palmPosition"];
        hand.palmVelocity           = (List<object>) ho["palmVelocity"];
        hand.palmWidth              = (double) ho["palmWidth"];
        hand.pinchStrength          = (double) ho["pinchStrength"];
        hand.pinchDistance          = (double) ho["pinchDistance"];
        hand.s                      = (double) ho["s"];
        hand.sphereCenter           = (List<object>) ho["sphereCenter"];
        hand.sphereRadius           = (double) ho["sphereRadius"];
        hand.stabilizedPalmPosition = (List<object>) ho["stabilizedPalmPosition"];
        hand.timeVisible            = (double) ho["timeVisible"];
        hand.type                   = (string) ho["type"];
        hand.wrist                  = (List<object>) ho["wrist"];

        return hand;
    }

    private FingerData ConstructFingerData(object f) {
        FingerData                 finger = new FingerData();
        Dictionary<string, object> fo     = (Dictionary<string, object>) f;

        finger.bases                 = (List<object>) fo["bases"];
        finger.btipPosition          = (List<object>) fo["btipPosition"];
        finger.carpPosition          = (List<object>) fo["carpPosition"];
        finger.dipPosition           = (List<object>) fo["dipPosition"];
        finger.direction             = (List<object>) fo["direction"];
        finger.extended              = (bool) fo["extended"];
        finger.handId                = (long) fo["handId"];
        finger.id                    = (long) fo["id"];
        finger.length                = (double) fo["length"];
        finger.mcpPosition           = (List<object>) fo["mcpPosition"];
        finger.pipPosition           = (List<object>) fo["pipPosition"];
        finger.stabilizedTipPosition = (List<object>) fo["stabilizedTipPosition"];
        finger.timeVisible           = (double) fo["timeVisible"];
        finger.tipPosition           = (List<object>) fo["tipPosition"];
        finger.tipVelocity           = (List<object>) fo["tipVelocity"];
        finger.tool                  = (bool) fo["tool"];
        finger.touchDistance         = (double) fo["touchDistance"];
        finger.type                  = (long) fo["type"];
        finger.width                 = (double) fo["width"];

        return finger;
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