using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace ws_GetResult_RemoteSigning
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
                ServiceGetResult_VNPT service = new ServiceGetResult_VNPT();
                service.SetStart(args);
                Console.WriteLine("Press any key to stop the service...");
                Console.ReadKey();
                service.SetStop();
            }
            else
            {
                ServiceBase[] ServicesToRun;
                ServicesToRun = new ServiceBase[] { new ServiceGetResult_VNPT() };
                ServiceBase.Run(ServicesToRun);
            }
#else
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new ServiceGetResult_VNPT()
            };
            ServiceBase.Run(ServicesToRun);
#endif
        }
    }
}
