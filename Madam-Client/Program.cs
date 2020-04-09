﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Madam_Client
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            //checks if the interactive debugger is running 
            //so VS can debug without installation as service
            if (Environment.UserInteractive)
            {
                Madam_Client.Service1 service1 = new Madam_Client.Service1();
                service1.TestStartupAndStop(null);
            }

            //else run as normal service
            else
            {
                ServiceBase[] ServicesToRun;
                ServicesToRun = new ServiceBase[]
                {
                new Service1()
                };
                ServiceBase.Run(ServicesToRun);
            }
        }


    }
}
