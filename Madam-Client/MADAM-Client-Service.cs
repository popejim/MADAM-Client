﻿using System;
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
using System.DirectoryServices;
using System.IO;
using System.Collections;
using System.Management;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml.Serialization;

namespace Madam_Client
{
    public partial class MadamService : ServiceBase
    {
        private ManualResetEvent _shutdownEvent = new ManualResetEvent(false);
        private Thread _connectThread;
        private Thread _findServerThread;
        public string Ipv4;
        public bool replyRecieved;
        public IPAddress serverAddress;
        public MadamService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            Console.WriteLine("Service Starting....");
            
            //setup new thread for socket listener
            _connectThread = new Thread(SocketListenerTcp);
            _connectThread.Name = "Socket Connection Thread";
            _connectThread.IsBackground = true;
            _connectThread.Start();

            //setup new thread for finding server
            _findServerThread = new Thread(FindServerUdp);
            _findServerThread.Name = "Find Server Thread";
            _findServerThread.IsBackground = true;
            _findServerThread.Start();

            //listen for network address change event

            NetworkChange.NetworkAddressChanged += new NetworkAddressChangedEventHandler(NetworkAddressChanged);

            CheckUsers();
        }

        protected override void OnStop()
        {
            //shutdown listener thread
            _shutdownEvent.Set();
            if (!_connectThread.Join(3000))
            { 
                _connectThread.Abort();
            }
            Console.WriteLine("Service Stopping....");
            //add method to send stopped notification to server
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
                SendIp(Ipv4);
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

        private void SocketListenerTcp()
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
                int keepGoing = 1;
                while (keepGoing == 1)
                {
                    listener.BeginAccept(new AsyncCallback(Recieve), listener);
                    IPAddress temp = localEndPoint.Address;
                    Thread.Sleep(60000);
                }
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
            //code executed when the client recieves a packet from the server
            Console.WriteLine("Connection to server established");
            Socket listener = (Socket)ar.AsyncState;
            Socket handler = listener.EndAccept(ar);
            serverAddress = ((IPEndPoint)handler.RemoteEndPoint).Address;
            replyRecieved = true;
            CheckUsers();   
            
            //add bit for reading the incoming packet
            
            //switch case for different messages to fire different events
            //adding users, updating info etc
        }

        private void FindServerUdp()
        {
            //sends a udp packet every ten seconds until it finds a server IP
            while (replyRecieved == false)
            {
                Socket testOut = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                int port = 42069;

                IPEndPoint broadcast = new IPEndPoint(IPAddress.Broadcast, port);
                byte[] data = Encoding.ASCII.GetBytes("find");

                testOut.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, 1);
                testOut.SendTo(data, broadcast);
                Thread.Sleep(10000);
            }

            
        }

        private void SendIp(string ip)
        {
            //sleep to ensure new address is up
            Thread.Sleep(15000);

            //create tcp client to the endpoint
            TcpClient client = new TcpClient(serverAddress.ToString(), 42069);
            NetworkStream nwStream = client.GetStream();
            byte[] bytesToSend = ASCIIEncoding.ASCII.GetBytes(ip);

            //---send the text---
            Console.WriteLine("Sending : " + ip);
            nwStream.Write(bytesToSend, 0, bytesToSend.Length);

            //---read back the text---
            byte[] bytesToRead = new byte[client.ReceiveBufferSize];
            int bytesRead = nwStream.Read(bytesToRead, 0, client.ReceiveBufferSize);
            Console.WriteLine("Received : " + Encoding.ASCII.GetString(bytesToRead, 0, bytesRead));
            Console.ReadLine();
            client.Close();
        }

        private void CheckUsers()
        {
            //gets information for all local users on the machine
            ManagementObjectSearcher findUsers = new ManagementObjectSearcher(@"SELECT * FROM Win32_UserAccount");
            ManagementObjectCollection users = findUsers.Get();
            List<Users> userList = new List<Users>();
            var localUsers = users.Cast<ManagementObject>().Where(
                u => (bool)u["LocalAccount"] == true &&
                     (bool)u["Disabled"] == false &&
                     (bool)u["Lockout"] == false &&
                     int.Parse(u["SIDType"].ToString()) == 1 &&
                     u["Name"].ToString() != "HomeGroupUser$");

            foreach (ManagementObject user in localUsers)
            {
                Users tempuser = new Users();
                tempuser.accounttype=("Account Type: " + user["AccountType"].ToString());
                tempuser.description = ("Description: " + user["Description"].ToString());
                tempuser.domain = ("Domain: " + user["Domain"].ToString());
                tempuser.fullName = ("Full Name: " + user["FullName"].ToString());
                tempuser.LocalAccount = ("Local Account: " + user["LocalAccount"].ToString());
                tempuser.name = ("Name: " + user["Name"].ToString());
                tempuser.PasswordExpire = ("Password Expires: " + user["PasswordExpires"].ToString());
                tempuser.SID = ("SID: " + user["SID"].ToString());
                tempuser.SidType = ("SID Type: " + user["SIDType"].ToString());
                tempuser.Status = ("Status: " + user["Status"].ToString());
                userList.Add(tempuser);
            }
            Thread.Sleep(10000);
            try
            {
                SendUsers(userList, serverAddress.ToString());
            }
            catch(Exception e)
            {
                Console.WriteLine(Environment.NewLine + "Sending users failed, retrying...");
                SendUsers(userList, serverAddress.ToString());
                return;
            }
        }

        private void SendUsers(List<Users> listToSend, string ip)
        {
            //create tcp client to the endpoint
            TcpClient client = new TcpClient(ip, 42073);
            NetworkStream nwStream = client.GetStream();
            XmlSerializer mySerializer = new XmlSerializer(typeof(List<Users>));
            mySerializer.Serialize(nwStream, listToSend);
            nwStream.Close();
            client.Close();
        }

    }
}
