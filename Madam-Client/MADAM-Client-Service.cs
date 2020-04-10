using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.IO;

namespace Madam_Client
{
    public partial class MadamService : ServiceBase
    {
        private ManualResetEvent _shutdownEvent = new ManualResetEvent(false);
        private Thread _listenerThread;
        public string Ipv4;
        public MadamService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            Console.WriteLine("Service Starting....");
            
            //setup new thread for socket listener
            _listenerThread = new Thread(SocketListener);
            _listenerThread.Name = "Socket Listener Thread";
            _listenerThread.IsBackground = true;
            _listenerThread.Start();
            //listen for network address change event
            NetworkChange.NetworkAddressChanged += new NetworkAddressChangedEventHandler(NetworkAddressChanged);
        }

        protected override void OnStop()
        {
            //shutdown listener thread
            _shutdownEvent.Set();
            if (!_listenerThread.Join(3000))
            { 
                _listenerThread.Abort();
            }
            Console.WriteLine("Service Stopping....");
        }

        private void NetworkAddressChanged(object sender, EventArgs e)
        {
            //gets list of all network interfaces
            NetworkInterface[] adapters = NetworkInterface.GetAllNetworkInterfaces();

            foreach (NetworkInterface n in adapters)
            {
                //gets the unicast ip's from each network interface
                foreach (UnicastIPAddressInformation ip in n.GetIPProperties().UnicastAddresses)
                {
                    //if the ip is internetwork ipv4, set this to a string
                    if (ip.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    {
                        Ipv4 = ip.Address.ToString();
                    }
                } 

                //print out results for debug
                Console.WriteLine("NetworkMonitor {0} is {1} IP {2}",n.Name, n.OperationalStatus, Ipv4);
            }

            //line breaks for debug
            Console.WriteLine(" ");
        }

        internal void TestStartupAndStop(string[] args)
        {
            //needed to run as debug, shouldnt effect when running as service
            this.OnStart(args);
            Console.ReadLine();
            this.OnStop();
        }

        private void SocketListener()
        {
            //make endpoint for listener on localhost, uses port 42069
            IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
            IPAddress localIp = host.AddressList[0];
            IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Any, 42069);

            //Make TCP/IP socket
            Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            try
            {
                Console.WriteLine("Listening for connection...");
                listener.Bind(localEndPoint);
                listener.Listen(100);
                listener.BeginAccept(new AsyncCallback(Recieve), listener);
            }

            catch (Exception e)
            {
                Console.WriteLine("Unexpected exception : {0}", e.ToString());
            }
            //listens for incoming connection on a socket until shutdown event fires, this happens on service stop
            //while (!_shutdownEvent.WaitOne(0))
            //{

            //}
        }

        private void Recieve(IAsyncResult ar)
        {
            Console.WriteLine("Connection to server established");
        }
    }
}
