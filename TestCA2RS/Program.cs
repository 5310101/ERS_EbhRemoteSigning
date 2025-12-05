using ERS_Domain.CAService;
using ERS_Domain.clsUtilities;
using ERS_Domain.CustomSigner.CA2CustomSigner;
using ERS_Domain.Model;
using ERS_Domain.Request;
using System;
using System.Security.Cryptography.X509Certificates;
using System.Xml;

namespace TestCA2RS
{
    internal class Program
    {
        static void Main(string[] args)
        {
            try
            {
                var CA2Service = new CA2SigningService();
                string user_id = "0101300842-999";

                //get cert
                var response_getcert = CA2Service.GetCertificates(user_id, Guid.NewGuid().ToString().Replace("-", "")).GetAwaiter().GetResult();
                string serial_number = response_getcert.data.user_certificates[1].serial_number;
                string certRaw = response_getcert.data.user_certificates[1].cert_data;
                X509Certificate2 cert = new X509Certificate2(Convert.FromBase64String(certRaw));


                //Ky thu
                //doc file roi tao hash ky
                string url_sign_hash = "https://rmsca2.nacencomm.vn/api/data/sign";
                string pathfilePDF = "C:\\Users\\quanna\\Desktop\\CA2RSTest\\sample-local-pdf.pdf";
                string pathfilePDFTemp = "C:\\Users\\quanna\\Desktop\\CA2RSTest\\sample-local-pdf_temp.pdf";
                string pathfileXML = "C:\\Users\\quanna\\Desktop\\CA2RSTest\\D02-TS-595.xml";

                Console.WriteLine("Choose File Type");
                string type = Console.ReadLine();
                if (type == "xml")
                {
                    //ky test xml
                    XmlElement signedInfo = CA2SignUtilities.CreateSignedInfoNode(pathfileXML, "");
                    string hash_to_sign_xml = CA2SignUtilities.CreateHashXmlToSign(signedInfo);
                    var listFiles = new FileToSign[]
                    {
                        new FileToSign
                        {
                            doc_id = "EBHTEST_XML",
                            file_type = "xml",
                            data_to_be_signed = hash_to_sign_xml,
                        }
                    };
                    string transaction_id = Guid.NewGuid().ToString().Replace("-", "");
                    DateTime signTime = DateTime.Now;
                    var res = CA2Service.SignHashValue(user_id, transaction_id, listFiles, serial_number, signTime).GetAwaiter().GetResult();
                    Console.WriteLine("GetResult");
                    string a = Console.ReadLine();
                    var res2 = CA2Service.GetSignedResult(user_id, transaction_id).GetAwaiter().GetResult();
                    //Ghep cks vao xml
                    if (res2 != null && res2.status_code == 200)
                    {
                        //signature value sample: O6jN+4fGUP6v6ZunAQ0WKKknGn4rvIdipAJ6ZDBbx6JX08vYIh9niM0PypXRpH/45g9qpuzv6Vwgl1jO52SieASzPX52hBJuse0eqYrTWsISyENbIFlKbtr7KzxWL+FMyZmfSfrt2mpq/1STzh16+R+hzCzmwJAW0FmqFsc/b66QRtTrZbwz1ANx5zJgTh7+MU3rD+S62AVTyeZL4reh2AAT4b/npB71/UPfQNy6KbTj2KNyS+K8SF/EOfvT6y+1U8vBfloH7vZQUOU2XrOPX0SWb76RhShyDH59B+ku74BXjXJJzruQ1wwPltK61hjKkxIWhvdLcP4xYO7VCt+DgQ==
                        string res_value = res2.data.signatures[0].signature_value;
                        CA2SignUtilities.AddSignatureXml(pathfileXML, signedInfo, res_value, certRaw, signTime, "//D02-TS/Cky");
                    }
                }
                else if (type == "pdf")
                {
                    //ky test pdf
                    var profile = CA2SignUtilities.CreateHashPdfToSign(certRaw, pathfilePDF, DateTime.Now);
                    var listFiles = new FileToSign[]
                    {
                        new FileToSign
                        {
                            doc_id = "EBHTEST_PDF",
                            file_type = "pdf",
                            data_to_be_signed = profile.HashValue.ToBase64String(),
                        }
                    };
                    string transaction_id = Guid.NewGuid().ToString().Replace("-", "");
                    DateTime signTime = DateTime.Now;
                    var res = CA2Service.SignHashValue(user_id, transaction_id, listFiles, serial_number, signTime).GetAwaiter().GetResult();
                    Console.WriteLine("GetResult");
                    Console.ReadLine();
                    var reskq = CA2Service.GetSignedResult(user_id, transaction_id).GetAwaiter().GetResult();
                    //Ghep cks vao pdf
                    if (reskq != null && reskq.status_code == 200)
                    {
                        CA2SignUtilities.AddSignaturePdf(profile, pathfilePDFTemp, reskq.data.signatures[0].signature_value);
                    }
                }



                Console.ReadLine();

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.ReadLine();
            }

        }
    }
}
