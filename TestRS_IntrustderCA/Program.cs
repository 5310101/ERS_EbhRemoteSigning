using IntrustCA_Domain;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace TestRS_IntrustCA
{
    internal class Program
    {
        private static string _pathXmlFile = "C:\\Users\\quanna\\Desktop\\TestCKS\\TK1-TS-595.xml";
        private static string _pathPdfFile = "C:\\Users\\quanna\\Desktop\\TestCKS\\dummy_01mB.pdf";

        private static string _saveDir = "C:\\Users\\quanna\\Desktop\\Desktop File\\SingedFiles";
        private static string _userName = "DSS.001091055387";

        static void Main(string[] args)
        {
            Console.InputEncoding = Encoding.UTF8;
            Console.OutputEncoding = Encoding.UTF8;
            Console.WriteLine("Test RemoteSigning by IntrustCA");
            var certs = IntrustRemoteSigningService.GetCertificate(_userName, "");
            if (certs == null)
            {
                Console.WriteLine("Khong tim thay certificate");
                return;
            }
            var cert = certs[0];
            IntrustRemoteSigningService rs = new IntrustRemoteSigningService(_userName, cert);
            while (true)
            {
                var success = rs.SignRemoteOneFile(_pathPdfFile, Path.Combine(_saveDir, Path.GetFileName(_pathPdfFile)));
                var success2 = rs.SignRemoteOneFile(_pathXmlFile, Path.Combine(_saveDir, Path.GetFileName(_pathXmlFile)));
                if (success == true)
                {
                    string a = Console.ReadLine();
                    if (a == "e") break;
                }
            }
            

            //string jsonData = File.ReadAllText("C:\\Users\\quanna\\Desktop\\signpdfdemo.json");
            //string jsonResponse = IntrustSigningCoreService.TestJsonRequestSign(jsonData);
        }
    }
}
