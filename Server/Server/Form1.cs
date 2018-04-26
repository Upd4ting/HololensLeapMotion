using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Server {
    public partial class Form1 : Form {
        //Leap Motion websokect APIs
        private const    string GET_FOCUS      = "{\"focused\" : \"true\"}";
        private const    string LOST_FOCUS     = "{\"focused\" : \"false\"}";
        private const    string BACKGROUND_ON  = "{\"background\" : \"true\"";
        private const    string BACKGROUND_OFF = "{\"background\" : \"false\"";
        private const    string GESTURES_ON    = "{\"enableGestures\" : \"true\"";
        private const    string GESTURES_OFF   = "{\"enableGestures\" : \"false\"";
        private const    string HMD_ON         = "{\"optimizeHMD\" : \"true\"";
        private const    string HMD_OFF        = "{\"optimizeHMD\" : \"false\"";

        private bool   _running;

        private TcpListener     _listener;
        private ClientWebSocket _webSocket;
        private Dictionary<TcpClient, int> _clients = new Dictionary<TcpClient, int>();
        private Dictionary<TcpClient, long> _clientDelay = new Dictionary<TcpClient, long>();

        public Form1() {
            InitializeComponent();
            _running = false;
        }

        private async void StartWS() {
            _webSocket = new ClientWebSocket();
            await _webSocket.ConnectAsync(new Uri("ws://" + ipText.Text + ":20307/v6.json"), CancellationToken.None);

            await SendString(_webSocket, BACKGROUND_ON, CancellationToken.None);
            await SendString(_webSocket, GET_FOCUS, CancellationToken.None);
            await SendString(_webSocket, HMD_ON, CancellationToken.None); // Optimize HWND because that's for hololens

            LogInformation("Connected to WebSocket of LeapMotion");
        }

        private void StartWSReceivingThread() {
            Task.Run(async () => {
                try {
                    string frame = await ReadString(_webSocket);

                    long milliseconds = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;

                    foreach (TcpClient client in _clients.Keys) {
                        long lastMilli = _clientDelay[client];
                        int fps = _clients[client];

                        if (milliseconds - lastMilli >= 1000 / fps) {
                            _clientDelay[client] = lastMilli;

                            LogInformation("Sending frame to client: " + (client.Client.RemoteEndPoint as IPEndPoint).Address.ToString());

                            BinaryWriter writer = new BinaryWriter(client.GetStream());
                            writer.Write(frame);
                        }
                    }
                } catch {
                    // Swallow exception (close exception)
                }
            });

            LogInformation("WebSocket receiving thread started");
        }

        private async void StopWS() {
            await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Just stop", CancellationToken.None);
            LogInformation("Stopping WebSocket");
        }

        private void StartSocket() {
            _running = true;

            _listener = new TcpListener(new IPEndPoint(IPAddress.Any, int.Parse(portText.Text)));
            _listener.Start();

            Task.Run(() => {
                while (_running) {
                    try {
                        TcpClient client = _listener.AcceptTcpClient();

                        Task.Run(() => {
                            while (_running) {
                                try {
                                    BinaryReader reader = new BinaryReader(client.GetStream());
                                    int fps = reader.ReadInt32();

                                    LogInformation("Client: " + (client.Client.RemoteEndPoint as IPEndPoint).Address.ToString()
                                                   + " asked for a frame rate: " + fps);

                                    if (fps == -1) {
                                        _clients.Remove(client);
                                        _clientDelay.Remove(client);
                                        client.Close();
                                        client.Dispose();
                                    } else {
                                        long milliseconds = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                                        _clients[client] = fps;
                                        _clientDelay[client] = milliseconds;
                                    }
                                } catch {
                                    // Swallow close exception
                                }
                            }
                        });
                    } catch {
                        // Swallow close exception
                    }
                }
            });

            LogInformation("Socket started");
        }

        private void StopSocket() {
            _listener.Stop();
            _listener = null;
            _running = false;
            LogInformation("Socket stopped");
        }

        private void LogInformation(string text) {
            informationText.Text += text;
            informationText.Text += "\n";
        }

        private async void Form1_FormClosing(object sender, FormClosingEventArgs e) {
            if (_running)
                _listener.Stop();
            await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Just close", CancellationToken.None);
        }

        private void button_Click(object sender, EventArgs e)
        {
            if (_running) {
                _running = false;
                StopSocket();
                StopWS();
                button.Text = "Start";
            } else {
                _running = true;
                StartSocket();
                StartWS();
                StartWSReceivingThread();
                button.Text = "Stop";
            }
        }

        public static Task SendString(ClientWebSocket ws, string data, CancellationToken cancellation)
        {
            var                encoded = Encoding.UTF8.GetBytes(data);
            ArraySegment<byte> buffer  = new ArraySegment<byte>(encoded, 0, encoded.Length);
            return ws.SendAsync(buffer, WebSocketMessageType.Text, true, cancellation);
        }

        public static async Task<string> ReadString(ClientWebSocket ws)
        {
            ArraySegment<byte> buffer = new ArraySegment<byte>(new byte[8192]);

            WebSocketReceiveResult result = null;

            using (MemoryStream ms = new MemoryStream())
            {
                do
                {
                    result = await ws.ReceiveAsync(buffer, CancellationToken.None);
                    ms.Write(buffer.Array, buffer.Offset, result.Count);
                } while (!result.EndOfMessage);

                ms.Seek(0, SeekOrigin.Begin);

                using (StreamReader reader = new StreamReader(ms, Encoding.UTF8))
                {
                    return reader.ReadToEnd();
                }
            }
        }
    }
}