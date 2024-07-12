using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace TcpIpServer_SampleClientUdp
{
    public partial class Form1 : Form
    {
        private const string DEFAULTIP = "224.1.1.255"; // Default multicast IP
        private const int DEFAULTDESTPORT = 1001;
        private const int DEFAULTOWNPORT = 1002;
        private const int DEFAULTSOURCEPORT = 11000;
        private const int DEFAULTTTL = 10; // Default TTL value

        private UdpClient _udpClient;
        private IPEndPoint _ipAddress;
        private Thread _rcvThread;
        private bool _isRunning = true;

        public Form1()
        {
            InitializeComponent();
        }

        private void cmd_send_Click(object sender, EventArgs e)
        {
            byte[] sendBuffer;

            try
            {
                int ttl = DEFAULTTTL;
                if (!string.IsNullOrEmpty(tbTTL.Text) && !int.TryParse(tbTTL.Text, out ttl))
                {
                    MessageBox.Show("Invalid TTL value. Please enter a valid integer.");
                    return;
                }

                _ipAddress = new IPEndPoint(IPAddress.Parse(txt_host.Text), Convert.ToInt32(txt_port.Text));
                _udpClient = new UdpClient(DEFAULTSOURCEPORT);
                _udpClient.Connect(_ipAddress);

                if (IPAddress.Parse(txt_host.Text).IsMulticast())
                {
                    _udpClient.Client.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, ttl);
                }
                else
                {
                    _udpClient.Client.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.IpTimeToLive, ttl);
                }

                sendBuffer = Encoding.ASCII.GetBytes(txt_send.Text);
                _udpClient.Send(sendBuffer, sendBuffer.Length);

                _udpClient.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("An unknown error occurred: " + ex.Message);
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            txt_host.Text = DEFAULTIP; // Set default host to multicast IP
            txt_port.Text = DEFAULTDESTPORT.ToString();
            tbTTL.Text = DEFAULTTTL.ToString(); // Set default TTL value
            rtb_rcv.Enabled = false;

            _rcvThread = new Thread(rcvThreadMethod);
            _rcvThread.IsBackground = true;
            _rcvThread.Start();
        }

        public delegate void rcvThreadCallback(string text);

        private void rcvThreadMethod()
        {
            byte[] rcvBuffer;
            IPEndPoint ipEndPoint = new IPEndPoint(IPAddress.Any, DEFAULTOWNPORT);
            UdpClient udpClient = new UdpClient();

            try
            {
                udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                udpClient.Client.Bind(ipEndPoint);
                udpClient.JoinMulticastGroup(IPAddress.Parse(DEFAULTIP));

                while (_isRunning)
                {
                    rcvBuffer = udpClient.Receive(ref ipEndPoint);
                    string receivedText = $"{DateTime.Now}: {Encoding.ASCII.GetString(rcvBuffer)}";
                    rtb_rcv.Invoke(new rcvThreadCallback(UpdateTextBox), receivedText);
                }
            }
            catch (SocketException ex)
            {
                if (_isRunning)
                {
                    MessageBox.Show("An error occurred while receiving data: " + ex.Message);
                }
            }
            finally
            {
                udpClient.DropMulticastGroup(IPAddress.Parse(DEFAULTIP));
                udpClient.Close();
            }
        }

        private void UpdateTextBox(string text)
        {
            rtb_rcv.Clear();
            rtb_rcv.AppendText(text + Environment.NewLine);
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            _isRunning = false;
            _rcvThread.Join(); // Ensure the thread has finished
        }
    }

    public static class IPAddressExtensions
    {
        public static bool IsMulticast(this IPAddress address)
        {
            byte firstOctet = address.GetAddressBytes()[0];
            return firstOctet >= 224 && firstOctet <= 239;
        }
    }
}
