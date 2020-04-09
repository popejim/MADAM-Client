﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.NetworkInformation;

namespace Madam_Client
{
    public partial class Service1 : ServiceBase
    {
        public string ipv4;
        public Service1()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            Console.WriteLine("Service Starting....");

            //listener for network address changing
            NetworkChange.NetworkAddressChanged += new NetworkAddressChangedEventHandler(NetworkAddressChanged);
        }

        protected override void OnStop()
        {
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
                        ipv4 = ip.Address.ToString();
                    }
                } 

                //print out results for debug
                Console.WriteLine("NetworkMonitor {0} is {1} IP {2}",n.Name, n.OperationalStatus, ipv4);
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


    }
}
