using IntrustCA_Domain;
using IntrustCA_Domain.Dtos;
using IntrustCA_Domain.Dtos;
using System;
using System.IO;
using System.Text;

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

            var certs = IntrustRSHelper.GetCertificates(_userName, "");
            if (certs == null)
            {
                Console.WriteLine("Khong tim thay certificate");
                return;
            }
            var cert = certs[0];
            var session = new SignSessionStore(_userName, cert);
            IntrustRemoteSigningService rs = new IntrustRemoteSigningService(session);
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
