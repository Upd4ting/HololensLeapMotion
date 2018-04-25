using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Server
{
    public partial class Form1 : Form {
        private TcpListener _listener;
        private ClientWebSocket _webSocket;
        private bool _running;

        public Form1()
        {
            InitializeComponent();
            _running = false;
        }

        private void StartWebSocket() {
            _webSocket = new ClientWebSocket();
        }

        private async void Form1_FormClosing(object sender, FormClosingEventArgs e) {
            if (_running)
                _listener.Stop();
            await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Just close", CancellationToken.None);
        }
    }
}
