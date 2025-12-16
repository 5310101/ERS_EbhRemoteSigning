using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace CA2_Winservice
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
#if DEBUG
            if (Environment.UserInteractive)
            {
                Console.WriteLine("Starting Service IntrustCA remote signing");
                wsCA2_RemoteSigning service = new wsCA2_RemoteSigning();
                service.ManualStart(args);
                Console.WriteLine("Service started. Press any key to stop...");
                Console.ReadKey();
                service.ManualStop();
            }
            else
            {
                ServiceBase[] ServicesToRun;
                ServicesToRun = new ServiceBase[]
                {
                new wsCA2_RemoteSigning()
                };
                ServiceBase.Run(ServicesToRun);
            }

#else
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new wsCA2_RemoteSigning()
            };
            ServiceBase.Run(ServicesToRun);

#endif
        }
    }
}

