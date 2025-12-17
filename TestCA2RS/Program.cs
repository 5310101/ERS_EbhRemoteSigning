using ERS_Domain.CAService;
using ERS_Domain.clsUtilities;
using ERS_Domain.CustomSigner.CA2CustomSigner;
using ERS_Domain.Request;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Xml;

namespace TestCA2RS
{
    internal class Program
    {
        public static bool ValidateXmlSignature(string xmlPath, out string message)
        {
            try
            {
                // 1. Load XML
                var xmlDoc = new XmlDocument
                {
                    PreserveWhitespace = true
                };
                xmlDoc.Load(xmlPath);

                // 2. Load Signature
                var signatureNodes = xmlDoc.GetElementsByTagName(
                    "Signature",
                    SignedXml.XmlDsigNamespaceUrl);

                if (signatureNodes.Count == 0)
                {
                    message = "Không tìm thấy chữ ký số trong XML";
                    return false;
                }

                var signedXml = new SignedXml(xmlDoc);
                signedXml.LoadXml((XmlElement)signatureNodes[0]);

                // 3. Validate SignatureValue + References (gộp)
                try
                {
                    if (!signedXml.CheckSignature())
                    {
                        message = "Chữ ký số không hợp lệ (XML đã bị sửa hoặc sai hash)";
                        return false;
                    }
                }
                catch (CryptographicException ex)
                {
                    message = $"Lỗi crypto khi verify chữ ký: {ex.Message}";
                    return false;
                }

                // 4. Lấy certificate
                var cert = signedXml.KeyInfo
                    .OfType<KeyInfoX509Data>()
                    .FirstOrDefault()?
                    .Certificates[0] as X509Certificate2;

                if (cert == null)
                {
                    message = "Không tìm thấy chứng thư số trong chữ ký";
                    return false;
                }

                // 5. Check hạn chứng thư
                var now = DateTime.Now;
                if (now < cert.NotBefore || now > cert.NotAfter)
                {
                    message = "Chứng thư số đã hết hạn hoặc chưa hiệu lực";
                    return false;
                }

                // 6. Validate trust chain
                var chain = new X509Chain
                {
                    ChainPolicy =
            {
                RevocationMode = X509RevocationMode.Online,
                RevocationFlag = X509RevocationFlag.ExcludeRoot,
                VerificationFlags = X509VerificationFlags.NoFlag
            }
                };

                if (!chain.Build(cert))
                {
                    var errors = chain.ChainStatus
                        .Select(s => s.StatusInformation.Trim());

                    message = "Chứng thư số không tin cậy: " +
                              string.Join(" | ", errors);
                    return false;
                }

                // 7. (Optional) SigningTime
                var signingTimeNode = xmlDoc
                    .GetElementsByTagName("SigningTime")
                    .Cast<XmlNode>()
                    .FirstOrDefault();

                if (signingTimeNode != null &&
                    DateTime.TryParse(signingTimeNode.InnerText, out var signingTime))
                {
                    if (signingTime < cert.NotBefore || signingTime > cert.NotAfter)
                    {
                        message = "Thời điểm ký nằm ngoài hiệu lực chứng thư số";
                        return false;
                    }
                }

                message = "Chữ ký số XML hợp lệ";
                return true;
            }
            catch (Exception ex)
            {
                message = $"Lỗi không xác định khi validate chữ ký: {ex.Message}";
                return false;
            }
        }

        static void Main(string[] args)
        {
            try
            {
                Console.OutputEncoding = Encoding.UTF8;
                Console.InputEncoding = Encoding.UTF8;
                var CA2Service = new CA2SigningService();
                string user_id = "0101300842-999";

                //get cert
                var response_getcert = CA2Service.GetCertificates(user_id, Guid.NewGuid().ToString().Replace("-", "")).GetAwaiter().GetResult();
                string serial_number = response_getcert.data.user_certificates[1].serial_number;
                string certRaw = response_getcert.data.user_certificates[1].cert_data;
                X509Certificate2 cert = new X509Certificate2(Convert.FromBase64String(certRaw));


                //Ky thu
                //doc file roi tao hash ky
                string pathfilePDF = "C:\\Users\\quanna\\Desktop\\CA2RSTest\\sample-local-pdf.pdf";
                string pathfilePDFTemp = "C:\\Users\\quanna\\Desktop\\CA2RSTest\\sample-local-pdf_temp.pdf";
                string pathfileXML = "C:\\Users\\quanna\\Desktop\\CA2RSTest\\BHXHDienTu.xml";
                string pathfileXMLTemp = "C:\\Users\\quanna\\Desktop\\CA2RSTest\\BHXHDienTu_temp.xml";

                string pathSignedXMl = "C:\\Users\\quanna\\Desktop\\CA2RSTest\\BaoHiemDienTu_1.xml";

                //Console.WriteLine("Valid");
                //ValidateXmlSignature(pathSignedXMl, out string mes);
                //Console.WriteLine(mes);
                //if( Console.ReadLine() == "c")
                //{
                //    return;
                //}

                Console.WriteLine("Choose File Type");
                string type = Console.ReadLine();
                if (type == "xml")
                {
                    //ky test xml
                    XmlElement signedInfo = CA2SignUtilities.CreateSignedInfoNode(pathfileXML, cert,"");
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
                        //var signedInfo1 = CA2SignUtilities.CreateSignedInfoNode(pathfileXML, cert, "");   
                        byte[] data = CA2SignUtilities.AddSignatureXmlWithData(pathfileXML, signedInfo, res_value, certRaw, signTime, "//Hoso/CKy_Dvi");
                        File.WriteAllBytes(pathfileXMLTemp, data);

                        //valid xml
                        XmlDocument docV = new XmlDocument(){ PreserveWhitespace = true };
                        docV.Load(pathfileXMLTemp);
                        var signedXml = new SignedXml(docV);
                        var nodeList = docV.GetElementsByTagName("Signature", SignedXml.XmlDsigNamespaceUrl);
                        signedXml.LoadXml((XmlElement)nodeList[0]);
                        bool valid = signedXml.CheckSignature();
                        if( ValidateXmlSignature(pathfileXMLTemp, out string message) == false)
                        {
                            Console.WriteLine(message);
                        }
                        Console.WriteLine("Valid");
                    }
                }
                else if (type == "pdf")
                {
                    //ky test pdf
                    string transaction_id = Guid.NewGuid().ToString().Replace("-", "");
                    var profile = CA2SignUtilities.CreateHashPdfToSign(certRaw, pathfilePDF, DateTime.Now, transaction_id, Guid.NewGuid().ToString().Replace("-", ""));
                    var listFiles = new FileToSign[]
                    {
                        new FileToSign
                        {
                            doc_id = profile.DocId,
                            file_type = "pdf",
                            data_to_be_signed = profile.HashValue.ToBase64String(),
                        }
                    };
                    
                    DateTime signTime = DateTime.Now;
                    var res = CA2Service.SignHashValue(user_id, transaction_id, listFiles, serial_number, signTime).GetAwaiter().GetResult();
                    Console.WriteLine("GetResult");
                    Console.ReadLine();
                    var reskq = CA2Service.GetSignedResult(user_id, transaction_id).GetAwaiter().GetResult();
                    //Ghep cks vao pdf
                    if (reskq != null && reskq.status_code == 200)
                    {
                        string signatureValue = reskq.data.signatures[0].signature_value;
                        if (CA2SignUtilities.ValidSignaturePDF(signatureValue, profile.HashValue, cert) == false)
                        {
                            Console.WriteLine("Signature invalid");
                            return;
                        }
                         //CA2SignUtilities.AddSignaturePdf(profile, pathfilePDFTemp, signatureValue);
                        byte[] data = CA2SignUtilities.AddSignaturePdfWithData(profile, signatureValue);
                        System.IO.File.WriteAllBytes(pathfilePDFTemp, data);    
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
