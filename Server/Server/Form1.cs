using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

using MiniJSON;

namespace Server {
    public partial class Form1 : Form {
        //Leap Motion websokect APIs
        private const    string                           GET_FOCUS      = "{\"focused\" : \"true\"}";
        private const    string                           LOST_FOCUS     = "{\"focused\" : \"false\"}";
        private const    string                           BACKGROUND_ON  = "{\"background\" : \"true\"";
        private const    string                           BACKGROUND_OFF = "{\"background\" : \"false\"";
        private const    string                           GESTURES_ON    = "{\"enableGestures\" : \"true\"";
        private const    string                           GESTURES_OFF   = "{\"enableGestures\" : \"false\"";
        private const    string                           HMD_ON         = "{\"optimizeHMD\" : \"true\"";
        private const    string                           HMD_OFF        = "{\"optimizeHMD\" : \"false\"";
        private readonly Dictionary<TcpClient, int>       _clients       = new Dictionary<TcpClient, int>();
        private readonly Dictionary<TcpClient, Stopwatch> _clientWatch   = new Dictionary<TcpClient, Stopwatch>();

        private TcpListener _listener;

        private bool            _running;
        private ClientWebSocket _webSocket;

        public Form1() {
            InitializeComponent();
            _running = false;
        }

        public static Task SendString(ClientWebSocket ws, string data, CancellationToken cancellation) {
            byte[]             encoded = Encoding.UTF8.GetBytes(data);
            ArraySegment<byte> buffer  = new ArraySegment<byte>(encoded, 0, encoded.Length);
            return ws.SendAsync(buffer, WebSocketMessageType.Text, true, cancellation);
        }

        public static async Task<string> ReadString(ClientWebSocket ws) {
            ArraySegment<byte> buffer = new ArraySegment<byte>(new byte[8192]);

            WebSocketReceiveResult result = null;

            using (MemoryStream ms = new MemoryStream()) {
                do {
                    result = await ws.ReceiveAsync(buffer, CancellationToken.None);
                    ms.Write(buffer.Array, buffer.Offset, result.Count);
                } while (!result.EndOfMessage);

                ms.Seek(0, SeekOrigin.Begin);

                using (StreamReader reader = new StreamReader(ms, Encoding.UTF8)) {
                    return reader.ReadToEnd();
                }
            }
        }

        private async Task StartWS() {
            _webSocket = new ClientWebSocket();
            await _webSocket.ConnectAsync(new Uri("ws://" + ipText.Text + ":6437/v6.json"), CancellationToken.None);

            await SendString(_webSocket, BACKGROUND_ON, CancellationToken.None);
            await SendString(_webSocket, GET_FOCUS,     CancellationToken.None);
            await SendString(_webSocket, HMD_ON,        CancellationToken.None); // Optimize HWND because that's for hololens

            LogInformation("Connected to WebSocket of LeapMotion");
        }

        private void StartWSReceivingThread() {
            Task.Run(async () => {
                while (_running)
                    try {
                        string frame = await ReadString(_webSocket);

                        FrameData? data = ConstructFrameData(frame);

                        if (!data.HasValue)
                            continue;

                        byte[] dataArray = DataToBytes(data.Value);

                        foreach (TcpClient client in _clients.Keys) {
                            Stopwatch watch = _clientWatch[client];
                            int       fps   = _clients[client];

                            watch.Stop();

                            if (watch.ElapsedMilliseconds >= 1000 / fps) {
                                BinaryWriter writer = new BinaryWriter(client.GetStream());
                                writer.Write(dataArray.Length);
                                writer.Write(dataArray);
                                watch.Reset();
                                watch.Start();
                            } else {
                                watch.Start();
                            }
                        }
                    } catch (Exception e) {
                        // Swallow exception (close exception)
                    }
            });

            LogInformation("WebSocket receiving thread started");
        }

        private async Task StopWS() {
            await _webSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None);
            LogInformation("Stopping WebSocket");
        }

        private void StartSocket() {
            _running = true;

            _listener = new TcpListener(new IPEndPoint(IPAddress.Any, int.Parse(portText.Text)));
            _listener.Start();

            Task.Run(() => {
                while (_running)
                    try {
                        TcpClient client = _listener.AcceptTcpClient();

                        LogInformation("Client connected");

                        Task.Run(() => {
                            while (_running && client.Connected)
                                try {
                                    BinaryReader reader = new BinaryReader(client.GetStream());
                                    int          fps    = reader.ReadInt32();

                                    LogInformation("Client: "                                                             + (client.Client.RemoteEndPoint as IPEndPoint).Address
                                                                                            + " asked for a frame rate: " + fps);

                                    if (fps == -1) {
                                        _clients.Remove(client);
                                        _clientWatch.Remove(client);
                                        client.Close();
                                    } else {
                                        _clients[client]     = fps;
                                        _clientWatch[client] = Stopwatch.StartNew();
                                    }
                                } catch {
                                    // Swallow close exception
                                }
                        });
                    } catch {
                        // Swallow close exception
                    }
            });

            LogInformation("Socket started");
        }

        private void StopSocket() {
            _listener.Stop();
            _listener = null;
            _running  = false;
            LogInformation("Socket stopped");
        }

        private void LogInformation(string text) {
            BeginInvoke(new MethodInvoker(delegate {
                informationText.Text += text;
                informationText.Text += "\n";
            }));
        }

        private async void Form1_FormClosing(object sender, FormClosingEventArgs e) {
            if (_running) {
                StopSocket();
                await StopWS();
            }
        }

        private async void button_Click(object sender, EventArgs e) {
            if (_running) {
                _running = false;
                StopSocket();
                await StopWS();
                button.Text = "Start";
            } else {
                _running = true;
                StartSocket();
                await StartWS();
                StartWSReceivingThread();
                button.Text = "Stop";
            }
        }

        private FrameData? ConstructFrameData(string res) {
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
            hand.elbox                  = (List<object>) ho["elbox"];
            hand.grapAngle              = (double) ho["grapAngle"];
            hand.grapStrength           = (double) ho["grapStrength"];
            hand.id                     = (long) ho["id"];
            hand.palmNormal             = (List<object>) ho["palmNormal"];
            hand.palmPosition           = (List<object>) ho["palmPosition"];
            hand.palmVelocity           = (List<object>) ho["palmVelocity"];
            hand.palmWidth              = (double) ho["palmWidth"];
            hand.confidence             = (double) ho["confidence"];
            hand.pinchStrength          = (double) ho["pinchStrength"];
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

            finger.boneBasics            = (List<object>) fo["boneBasics"];
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
    }
}