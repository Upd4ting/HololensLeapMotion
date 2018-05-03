using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Runtime.Serialization.Formatters.Binary;
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

                        foreach (TcpClient client in _clients.Keys) {
                            Stopwatch watch = _clientWatch[client];
                            int       fps   = _clients[client];

                            watch.Stop();

                            if (watch.ElapsedMilliseconds >= 1000 / fps) {
                                BinaryWriter writer = new BinaryWriter(client.GetStream());
                                writer.Write(frame);
                                watch.Reset();
                                watch.Start();
                            } else {
                                watch.Start();
                            }
                        }
                    } catch (Exception e) {
                        Console.WriteLine(e.Message);
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
                                } catch (Exception e) {
                                    Console.WriteLine(e.Message);
                                    // Swallow close exception
                                }
                        });
                    } catch (Exception e) {
                        Console.WriteLine(e.Message);
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
    }
}