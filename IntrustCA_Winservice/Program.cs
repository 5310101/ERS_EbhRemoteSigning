using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace IntrustCA_Winservice
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
                wsIntrustCA_RemoteSigning service = new wsIntrustCA_RemoteSigning();
                //test
                //var res = IntrustCA_Domain.IntrustSigningCoreService.GetCertificate(new IntrustCA_Domain.Dtos.GetCertificateRequest
                //{
                //    user_id = "DSS.001091055387",
                //    serial_number = ""
                //});

                service.StartManual(args);
                Console.WriteLine("Service started. Press any key to stop...");
                Console.ReadKey();  
                service.StopManual();
            }
            else
            {
                ServiceBase[] ServicesToRun;
                ServicesToRun = new ServiceBase[]
                {
                new wsIntrustCA_RemoteSigning()
                };
                ServiceBase.Run(ServicesToRun);
            }

#else
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new wsIntrustCA_RemoteSigning()
            };
            ServiceBase.Run(ServicesToRun);

#endif
        }
    }
}
