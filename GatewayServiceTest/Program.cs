using GatewayServiceTest.Signature;
using iTextSharp.text;
using log4net;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web.ModelBinding;
using System.Xml;
using VnptHashSignatures.Common;
using VnptHashSignatures.Interface;
using VnptHashSignatures.Office;
using VnptHashSignatures.Pdf;
using VnptHashSignatures.Xml;

namespace GatewayServiceTest
{
    class Program
    {
        // Logger for this class
        private static readonly ILog _log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            // log4net configuration
            log4net.Config.XmlConfigurator.Configure();
            
            _signSmartCAPDF();
            //_signSmartCAOFFICE();
            //_signSmartCAXML();            
            Console.ReadKey();
        }

        private static void _signSmartCAPDF()
        {
            var customerEmail = "162952530";// "03090010105"; 
            var customerPass = "871097";// "123456aA@";
            var access_token = CoreServiceClient.GetAccessToken(customerEmail, customerPass, out string refresh_token);


            String credential = _getCredentialSmartCA(access_token, "https://rmgateway.vnptit.vn/csc/credentials/list");
            //String credential = _getCredentialSmartCA(access_token, "https://gwsca.vnpt.vn/csc/credentials/list");
            String certBase64 = _getAccoutSmartCACert(access_token, "https://rmgateway.vnptit.vn/csc/credentials/info", credential);
            //String certBase64 = _getAccoutSmartCACert(access_token, "https://gwsca.vnpt.vn/csc/credentials/info", credential);

            string _pdfInput = @"C:\Users\Hung Vu\Desktop\test.pdf";
            string _pdfSignedPath = @"C:\Users\Hung Vu\Desktop\test_signed.pdf";

            byte[] unsignData = null;
            try
            {
                unsignData = File.ReadAllBytes(_pdfInput);
            }
            catch (Exception ex)
            {
                _log.Error(ex);
                return;
            }


            //SignHash Begin            
            
            IHashSigner signer = HashSignerFactory.GenerateSigner(unsignData, certBase64, null, HashSignerFactory.PDF);
            ((PdfHashSigner)signer).SetHashAlgorithm(MessageDigestAlgorithm.SHA256);

            
            #region Optional -----------------------------------
            // Property: Lý do ký số
            ((PdfHashSigner)signer).SetReason("Xác nhận tài liệu");
            // Hình ảnh hiển thị trên chữ ký (mặc định là logo VNPT)
            var imgBytes = File.ReadAllBytes(@"C:\Users\Hung Vu\Desktop\Logo_MISA.jpg");
            var x = Convert.ToBase64String(imgBytes);
            ((PdfHashSigner)signer).SetCustomImage(imgBytes);
            // Signing page (@deprecated)
            //((PdfHashSigner)signer).SetSigningPage(1);
            // Vị trí và kích thước chữ ký (@deprecated)
            //((PdfHashSigner)signer).SetSignaturePosition(20, 20, 220, 50);
            // Kiểu hiển thị chữ ký (OPTIONAL/DEFAULT=TEXT_WITH_BACKGROUND)
            ((PdfHashSigner)signer).SetRenderingMode(PdfHashSigner.RenderMode.TEXT_WITH_BACKGROUND);
            // Nội dung text trên chữ ký (OPTIONAL)
            //((PdfHashSigner)signer).SetLayer2Text("Ký bởi: Subject name\nNgày ký: Datetime.now");
            // Fontsize cho text trên chữ ký (OPTIONAL/DEFAULT = 10)
            ((PdfHashSigner)signer).SetFontSize(10);
            ((PdfHashSigner)signer).SetLayer2Text("yahooooooooooooooooooooooooooo");
            // Màu text trên chữ ký (OPTIONAL/DEFAULT=000000)
            ((PdfHashSigner)signer).SetFontColor("0000ff");
            // Kiểu chữ trên chữ ký
            ((PdfHashSigner)signer).SetFontStyle(PdfHashSigner.FontStyle.Normal);
            // Font chữ trên chữ ký
            ((PdfHashSigner)signer).SetFontName(PdfHashSigner.FontName.Times_New_Roman);

            //Hiển thị chữ ký và vị trí bên dưới dòng _textFilter cách 1 đoạn _marginTop, độ rộng _width, độ cao _height
            //using (var reader = new PdfReader(unsignData))
            //{

            //    var parser = new PdfReaderContentParser(reader);

            //    for (int pageNum = 1; pageNum <= reader.NumberOfPages; ++pageNum)
            //    {
            //        var strategy = parser.ProcessContent(pageNum, new LocationTextExtractionStrategyWithPosition());

            //        var res = strategy.GetLocations();

            //        var post = new TextLocation();

            //        foreach (TextLocation textContent in res)
            //        {
            //            if (textContent.Text.Contains(_textFilter))
            //            {
            //                ((PdfHashSigner)signer).AddSignatureView(new PdfSignatureView
            //                {
            //                    Rectangle = string.Format("{0},{1},{2},{3}", (int)textContent.X, (object)(int)(textContent.Y - _marginTop - _height), (int)(textContent.X + _width), (int)(textContent.Y - _marginTop)),
            //                    Page = pageNum
            //                });
            //            }
            //        }
            //    }



            //    reader.Close();
            //    //var searchResult = res.Where(p => p.Text.Contains(searchText)).OrderBy(p => p.Y).Reverse().ToList();
            //}            
            
            // Hiển thị ảnh chữ ký tại nhiều vị trí trên tài liệu
            ((PdfHashSigner)signer).AddSignatureView(new PdfSignatureView
            {
                Rectangle = "49,488,269,554",
                Page = 1
            });

            //((PdfHashSigner)signer).AddSignatureComment(new PdfSignatureComment
            //{
            //    Type = (int)PdfSignatureComment.Types.TEXT,
            //    Text = "This is comment",
            //    Page = 1,
            //    Rectangle = "20,20,220,50",
            //});

            // Thêm comment vào dữ liệu
            ((PdfHashSigner)signer).AddSignatureComment(new PdfSignatureComment
            {
                Page = 1,
                Rectangle = "92,609,292,630",
                Text = "yahohohohohohohhodsánlfn",
                FontName = PdfHashSigner.FontName.Times_New_Roman,
                FontSize = 13,
                FontColor = "0000FF",
                FontStyle = 2,
                Type = (int)PdfSignatureComment.Types.TEXT,
            });

            // Signature widget border type (OPTIONAL)
            ((PdfHashSigner)signer).SetSignatureBorderType(PdfHashSigner.VisibleSigBorder.DASHED);
            #endregion -----------------------------------------
            
            var hashValue = signer.GetSecondHashAsBase64();                             

            var tranId = _signHash(access_token, "https://rmgateway.vnptit.vn/csc/signature/signhash", hashValue, credential);
            //var tranId = _signHash(access_token, "https://gwsca.vnpt.vn/csc/signature/signhash", hashValue, credential);
            //SignHash End
            
            //Sign Begin
            //var tranId = _sign(access_token, "https://rmgateway.vnpt.vn/csc/signature/sign", Convert.ToBase64String(unsignData), credential);
            //Sign End

            if (tranId == "")
            {
                _log.Error("Ky so that bai");
                return;
            }

            var count = 0;
            var isConfirm = false;
            var datasigned = "";
            while(count < 24 && !isConfirm)
            {
                _log.Info("Get TranInfo PDF lan "+ count + " : ");
                var tranInfo = _getTranInfo(access_token, "https://rmgateway.vnptit.vn/csc/credentials/gettraninfo", tranId);
                //var tranInfo = _getTranInfo(access_token, "https://gwsca.vnpt.vn/csc/credentials/gettraninfo", tranId);
                if (tranInfo != null)
                {
                    if (tranInfo.tranStatus != 1)
                    {
                        count = count + 1;
                        Thread.Sleep(10000);
                    }
                    else
                    {
                        isConfirm = true;
                        datasigned = tranInfo.documents[0].sig;
                    }
                }
                else
                {
                    _log.Error("Error from content");
                    return;
                }
            }
            if (!isConfirm)
            {
                _log.Error("Signer not confirm from App");
                return;
            }

            if (string.IsNullOrEmpty(datasigned))
            {
                _log.Error("Sign error");
                return;
            }
            
            
            if (!signer.CheckHashSignature(datasigned))
            {
                _log.Error("Signature not match");
                return;
            }
            // ------------------------------------------------------------------------------------------

            // 3. Package external signature to signed file
            byte[] signed = signer.Sign(datasigned);            
            File.WriteAllBytes(_pdfSignedPath, signed);
            

            //File.WriteAllBytes(_pdfSignedPath, Convert.FromBase64String(datasigned));
            
            _log.Info("SignHash PDF: Successfull. signed file at '" + _pdfSignedPath + "'");

        }


        private static void _signSmartCAOFFICE()
        {
            var customerEmail = "162952530";// "03090010105"; 
            var customerPass = "871097";// "123456aA@";
            var access_token = CoreServiceClient.GetAccessToken(customerEmail, customerPass, out string refresh_token);


            String credential = _getCredentialSmartCA(access_token, "https://rmgateway.vnptit.vn/ssa/credentials/list");
            //String credential = _getCredentialSmartCA(access_token, "https://gwsca.vnpt.vn/csc/credentials/list");
            String certBase64 = _getAccoutSmartCACert(access_token, "https://rmgateway.vnptit.vn/ssa/credentials/info", credential);
            //String certBase64 = _getAccoutSmartCACert(access_token, "https://gwsca.vnpt.vn/csc/credentials/info", credential);

            string _pdfInput = @"C:\Users\Hung Vu\Desktop\test.docx";
            string _pdfSignedPath = @"C:\Users\Hung Vu\Desktop\test_signed.docx";

            byte[] unsignData = null;
            try
            {
                unsignData = File.ReadAllBytes(_pdfInput);
            }
            catch (Exception ex)
            {
                _log.Error(ex);
                return;
            }

            
            IHashSigner signers = HashSignerFactory.GenerateSigner(unsignData, certBase64, null, HashSignerFactory.OFFICE);
            
            ((OfficeHashSigner)signers).SetHashAlgorithm(MessageDigestAlgorithm.SHA256);

            
            string hashValues = null;
            try
            {
                hashValues = signers.GetSecondHashAsBase64();
            }
            catch (Exception ex)
            {
                _log.Error(ex);
                return;
            }            
            

            var tranId = _signHash(access_token, "https://rmgateway.vnptit.vn/ssa/signature/signhash", hashValues, credential);
            //var tranId = _signHash(access_token, "https://gwsca.vnpt.vn/csc/signature/signhash", hashValue, credential);
            //SignHash End
            

            if (tranId == "")
            {
                _log.Error("Ky so that bai");
                return;
            }

            var count = 0;
            var isConfirm = false;
            var datasigned = "";
            while (count < 24 && !isConfirm)
            {
                _log.Info("Get TranInfo PDF lan " + count + " : ");
                var tranInfo = _getTranInfo(access_token, "https://rmgateway.vnptit.vn/ssa/credentials/gettraninfo", tranId);
                //var tranInfo = _getTranInfo(access_token, "https://gwsca.vnpt.vn/csc/credentials/gettraninfo", tranId);
                if (tranInfo != null)
                {
                    if (tranInfo.tranStatus != 1)
                    {
                        count = count + 1;
                        Thread.Sleep(10000);
                    }
                    else
                    {
                        isConfirm = true;
                        datasigned = tranInfo.documents[0].signature;
                    }
                }
                else
                {
                    _log.Error("Error from content");
                    return;
                }
            }
            if (!isConfirm)
            {
                _log.Error("Signer not confirm from App");
                return;
            }

            if (string.IsNullOrEmpty(datasigned))
            {
                _log.Error("Sign error");
                return;
            }


            if (!signers.CheckHashSignature(datasigned))
            {
                _log.Error("Signature not match");
                return;
            }
            // ------------------------------------------------------------------------------------------

            // 3. Package external signature to signed file
            byte[] signed = signers.Sign(datasigned);
            File.WriteAllBytes(_pdfSignedPath, signed);


            //File.WriteAllBytes(_pdfSignedPath, Convert.FromBase64String(datasigned));

            _log.Info("SignHash PDF: Successfull. signed file at '" + _pdfSignedPath + "'");

        }

        private static void _signSmartCAXML()
        {
            var customerEmail = "162952530";// "03090010105"; 
            var customerPass = "871097";// "123456aA@";
            var access_token = CoreServiceClient.GetAccessToken(customerEmail, customerPass, out string refresh_token);


            String credential = _getCredentialSmartCA(access_token, "https://rmgateway.vnptit.vn/ssa/credentials/list");
            //String credential = _getCredentialSmartCA(access_token, "https://gwsca.vnpt.vn/csc/credentials/list");
            String certBase64 = _getAccoutSmartCACert(access_token, "https://rmgateway.vnptit.vn/ssa/credentials/info", credential);
            //String certBase64 = _getAccoutSmartCACert(access_token, "https://gwsca.vnpt.vn/csc/credentials/info", credential);

            string _pdfInput = @"C:\Users\Hung Vu\Desktop\test.xml";
            string _pdfSignedPath = @"C:\Users\Hung Vu\Desktop\test_signed.xml";

            byte[] unsignData = null;
            try
            {
                unsignData = File.ReadAllBytes(_pdfInput);
            }
            catch (Exception ex)
            {
                _log.Error(ex);
                return;
            }

            
            IHashSigner signers = HashSignerFactory.GenerateSigner(unsignData, certBase64, null, HashSignerFactory.XML);
            
            ((XmlHashSigner)signers).SetHashAlgorithm(MessageDigestAlgorithm.SHA256);

            //Các lựa chọn ký vào thẻ có id
            ((XmlHashSigner)signers).SetSignatureID("seller");

            //Set reference đến id
            ((XmlHashSigner)signers).SetReferenceId("#SigningData");

            //Set thời gian ký
            ((XmlHashSigner)signers).SetSigningTime(DateTime.UtcNow.AddDays(-5));
            
            string hashValues = null;
            try
            {
                hashValues = signers.GetSecondHashAsBase64();
            }
            catch (Exception ex)
            {
                _log.Error(ex);
                return;
            }            

            var tranId = _signHash(access_token, "https://rmgateway.vnptit.vn/ssa/signature/signhash", hashValues, credential);
            //var tranId = _signHash(access_token, "https://gwsca.vnpt.vn/csc/signature/signhash", hashValue, credential);
            //SignHash End            
            if (tranId == "")
            {
                _log.Error("Ky so that bai");
                return;
            }

            var count = 0;
            var isConfirm = false;
            var datasigned = "";
            while (count < 24 && !isConfirm)
            {
                _log.Info("Get TranInfo PDF lan " + count + " : ");
                var tranInfo = _getTranInfo(access_token, "https://rmgateway.vnptit.vn/ssa/credentials/gettraninfo", tranId);
                //var tranInfo = _getTranInfo(access_token, "https://gwsca.vnpt.vn/csc/credentials/gettraninfo", tranId);
                if (tranInfo != null)
                {
                    if (tranInfo.tranStatus != 1)
                    {
                        count = count + 1;
                        Thread.Sleep(10000);
                    }
                    else
                    {
                        isConfirm = true;
                        datasigned = tranInfo.documents[0].signature;
                    }
                }
                else
                {
                    _log.Error("Error from content");
                    return;
                }
            }
            if (!isConfirm)
            {
                _log.Error("Signer not confirm from App");
                return;
            }

            if (string.IsNullOrEmpty(datasigned))
            {
                _log.Error("Sign error");
                return;
            }


            if (!signers.CheckHashSignature(datasigned))
            {
                _log.Error("Signature not match");
                return;
            }
            // ------------------------------------------------------------------------------------------

            // 3. Package external signature to signed file
            byte[] signed = signers.Sign(datasigned);
            File.WriteAllBytes(_pdfSignedPath, signed);


            //File.WriteAllBytes(_pdfSignedPath, Convert.FromBase64String(datasigned));

            _log.Info("SignHash PDF: Successfull. signed file at '" + _pdfSignedPath + "'");

        }

        private static TranInfoSmartCARespContent _getTranInfo(string accessToken, String serviceUri, String tranId){
            var response = CoreServiceClient.Query(new ContenSignHash
            {
                tranId = tranId
            }, serviceUri, accessToken);

            if (response != null)
            {
                TranInfoSmartCAResp resp = JsonConvert.DeserializeObject<TranInfoSmartCAResp>(response);
                if (resp.code == 0)
                {
                    return resp.content;
                }
            }
            return null;
        }        

        private static string _signHash(string accessToken, String serviceUri, string data, string credentialId)
        {
            var req = new SignHashSmartCAReq
            {
                credentialId = credentialId,
                refTranId = "1234-5678-90000",
                notifyUrl = "http://10.169.0.221/api/v1/smart_ca_callback",
                description = "Test for docx",
                datas = new List<DataSignHash>()
            };
            var test = new DataSignHash
            {
                name = "test.docx",
                hash = data
            };
            req.datas.Add(test);

            var response = CoreServiceClient.Query(req, serviceUri, accessToken);
            if (response != null)
            {
                SignHashSmartCAResponse resp = JsonConvert.DeserializeObject<SignHashSmartCAResponse>(response);
                if(resp.code == 0)
                {
                    return resp.content.tranId;
                }
            }
            return "";            
        }

        private static string _sign(string accessToken, String serviceUri, string data, string credentialId)
        {
            var req = new SignSmartCAReq
            {
                credentialId = credentialId,
                description = "Test for pdf",
                datas = new List<DataSign>()
            };
            var test = new DataSign
            {
                name = "test.pdf",
                dataBase64 = data
            };
            req.datas.Add(test);

            var response = CoreServiceClient.Query(req, serviceUri, accessToken);
            if (response != null)
            {
                SignHashSmartCAResponse resp = JsonConvert.DeserializeObject<SignHashSmartCAResponse>(response);
                if (resp.code == 0)
                {
                    return resp.content.tranId;
                }
            }
            return "";            
        }

        private static string _getCredentialSmartCA(String accessToken, String serviceUri)
        {
            var response = CoreServiceClient.Query(new ReqCredentialSmartCA(), serviceUri, accessToken);
            
            if (response != null)
            {
                CredentialSmartCAResponse credentials = JsonConvert.DeserializeObject<CredentialSmartCAResponse>(response);
                return credentials.content[0];
            }
            return "";
        }
        private static String _getAccoutSmartCACert(String accessToken, String serviceUri, string credentialId)
        {
            var response = CoreServiceClient.Query(new ReqCertificateSmartCA {
                credentialId = credentialId,
                certificates = "chain",
                certInfo = true,
                authInfo = true
            }, serviceUri, accessToken);
            if (response != null)
            {
                CertificateSmartCAResponse req = JsonConvert.DeserializeObject<CertificateSmartCAResponse>(response);
                String certBase64 = req.cert.certificates[0];
                return certBase64.Replace("\r\n", "");
            }
            return "";
        }

        private static void _buildFile()
        {
            string _pdfSignedPath = @"C:\Users\Hung Vu\Desktop\test_signed.pdf";
            string dataHash = "";
            
            string _pdfInput = @"C:\Users\Hung Vu\Desktop\test.pdf";
            byte[] unsignData = null;
            try
            {
                unsignData = File.ReadAllBytes(_pdfInput);
            }
            catch (Exception ex)
            {
                _log.Error(ex);
                return;
            }
            string certBase64 = "MIIEsDCCA5igAwIBAgIQVAEBATzXYNIcB0Y1K4X2sjANBgkqhkiG9w0BAQsFADBeMRUwEwYDVQQDDAxWTlBUIFNNQVJUQ0ExIzAhBgNVBAsMGlZOUFQtU01BUlRDQSBUcnVzdCBOZXR3b3JrMRMwEQYDVQQKDApWTlBUIEdyb3VwMQswCQYDVQQGEwJWTjAeFw0yMTA5MDExMjA0MDBaFw0yMjA5MDIwMDA0MDBaMHExCzAJBgNVBAYTAlZOMRIwEAYDVQQIDAlIw4AgTuG7mEkxDzANBgNVBAcMBlF14bqtbjEaMBgGA1UEAwwRTmfDtCBRdWFuZyDEkOG6oXQxITAfBgoJkiaJk/IsZAEBDBFDTU5EOjAzNjA4ODAwNzk1NjCCASIwDQYJKoZIhvcNAQEBBQADggEPADCCAQoCggEBAMnTFAxSDlXb8CxvzpfONSGBD8rabblF/l+ObaZvSQchPyDA+jJL4Ja/CwNqxB3VKwUdQtywygGWAbyP0ETQa7MM556yUlUHKUQom5rmYe96rho0Rkw02/GMEDiRPbnWZjiNHb0ZKABRXvRR/Ta/ure/jDzaEdB+Hb3j+Rwc9yL4Vgdp3ZISs55CkIBUcXtBmnunjms6da34H+s5nfWXga6LMWK0JACR16fBTqPZopHmZ4VIc+7W0zI8H9qk41xDRjqVeFGU7v18SIEsufpaDhwIx9PTVpAq83tYCgaZ0UKJx6GOMB35Gk/aDIIRIzYnKBbriLyg20Am1WV2RwfwUeECAwEAAaOCAVUwggFRMEIGCCsGAQUFBwEBBDYwNDAyBggrBgEFBQcwAoYmaHR0cDovL3B1Yi52bnB0LWNhLnZuL2NlcnRzL3ZucHRjYS5jZXIwHQYDVR0OBBYEFISje/wbEkeO4Q5CyxspwSdWavISMAwGA1UdEwEB/wQCMAAwHwYDVR0jBBgwFoAUy1hgYT690w3jArGEnTGmdNW7c4owbAYDVR0gBGUwYzBhBg4rBgEEAYHtAwEBAwEEAjBPMCYGCCsGAQUFBwICMBoeGABQAEkARAAtAFMAMQAuADAALQAwADMAbTAlBggrBgEFBQcCARYZaHR0cDovL3B1Yi52bnB0LWNhLnZuL3JwYTAOBgNVHQ8BAf8EBAMCBPAwHwYDVR0lBBgwFgYIKwYBBQUHAwQGCisGAQQBgjcKAwwwHgYDVR0RBBcwFYETbmdvcXVhbmdkYXRAdm5wdC52bjANBgkqhkiG9w0BAQsFAAOCAQEAXJ8jYyaLpKoDX+DszXd1Tk8hK7EEL/p355pXP6YT/FP2J9aBb4iyeidBShgjHxQ9IZTONA2+fr3JT0GlbfmnhzIC8vKppTkLLgt7ezVcfFEvQLDHyjXpynPWID2k/GlBe6N2/p67vjoSpeLIsdOZNLJAL2PaFhVnHGc+IWnjX8YS6CVCm3mSlTw/zDdKIG8YvyEBXeaST+WdVfET4TMNOLY4zSBVwWFZW1kzSs/zTHuSU90XQjoCYlvqimwEuxXHVIzJ14pLMoZk7FPx4DP4PXBCf/g092MAakzXD7lHstOcKi8xR71EBfkjoPvqpcUjrF5K7ZPVUN+qUwHNVMOhKw==";

            IHashSigner signer = HashSignerFactory.GenerateSigner(unsignData, certBase64, null, HashSignerFactory.PDF);

            if (!signer.CheckHashSignature(dataHash))
            {
                _log.Error("Signature not match");
                return;
            }
            byte[] signed = signer.Sign(dataHash);
            File.WriteAllBytes(_pdfSignedPath, signed);
            _log.Info("SignHash PDF: Successfull. signed file at '" + _pdfSignedPath + "'");

        }

        /// <summary>
        /// 
        /// </summary>
        private static void _signExample()
        {
            /* Account information ------------------------------------------------------------- */
            //Enterprise account
            var enterpriseEmil = Properties.Settings.Default["ENTERPRISE_ACC"] as string;
            // User account information
            var customerEmail = Properties.Settings.Default["USER_ACC"] as string;
            var customerPass = Properties.Settings.Default["USER_PASS"] as string;
            /* --------------------------------------------------------------------------------- */

            /* Authorize process --------------------------------------------------------------- */
            // 1. User login and get access_token, refresh_token
            var access_token = CoreServiceClient.GetAccessToken(customerEmail, customerPass, out string refresh_token);

            // 1.1. Get access_token from refresh_token when acess_token expired
            //access_token = CoreServiceClient.RefreshToken(refresh_token, out string new_refresh_token);
            /* --------------------------------------------------------------------------------- */


            /* Get certificate information ----------------------------------------------------- */
            // 2. Get valid certificate
            Certificate cert = _getAccoutCert(access_token);

            if (cert == null)
            {
                _log.Error("No valid certificate");
                Console.ReadLine();
                return;
            }

            _log.Info($"Cert ID={cert.ID}");
            _log.Info($"Cert base64={cert.CertBase64}");
            string certBase64 = _parseCert(cert.CertBase64);
            /* --------------------------------------------------------------------------------- */

            /* Get group information ----------------------------------------------------- */
            // 3. Lấy thông tin thanh toán giao dịch
            string groupId = _getGroupId(enterpriseEmil, access_token);
            if (string.IsNullOrEmpty(groupId))
            {
                _log.Error("No valid group");
                Console.ReadLine();
                return;
            }
            _log.Info($"Group ID={groupId}");
            /* --------------------------------------------------------------------------------- */

            /* Create signature process -------------------------------------------------------- */
            // 4. Sign pdf hashed file
            SignHash.SignHashPdf(groupId, cert.ID, certBase64, access_token);

            //for(int i = 0; i < 10; i++)
            //{
            //    Thread t = new Thread(() => SignMultipleHash.SignMultipleHashs(groupId, cert.ID, certBase64, access_token));
            //    t.Start();
            //}

            //SignHash.SignHashXml(groupId, cert.ID, certBase64, access_token);
            //SignFile.SignXml(cert.ID, access_token);

            //SignHash.SignHashOffice(groupId, cert.ID, certBase64, access_token);
            //SignFile.SignPdf(cert.ID, access_token);
            /* --------------------------------------------------------------------------------- */
            Console.ReadLine();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="certBase64"></param>
        /// <returns></returns>
        private static string _parseCert(string certBase64)
        {
            if (string.IsNullOrEmpty(certBase64))
            {
                return certBase64;
            }

            certBase64 = certBase64.Replace("-----BEGIN CERTIFICATE-----", "");
            certBase64 = certBase64.Replace("-----END CERTIFICATE-----", "");

            return certBase64;
        }

        /// <summary>
        /// Connect gateway and get valid certificates for account match access_token
        /// </summary>
        /// <param name="access_token"></param>
        /// <returns></returns>
        private static Certificate _getAccoutCert(string access_token)
        {
            var response = CoreServiceClient.Query(new RequestMessage
            {
                RequestID = Guid.NewGuid().ToString(),
                ServiceID = "Certificate",
                FunctionName = "GetAccountCertificateByEmail",
                Parameter = new CertParameter
                {
                    PageIndex = 0,
                    PageSize = 10
                }
            }, access_token);
            if (response != null)
            {
                var str = JsonConvert.SerializeObject(response.Content);
                CertResponse acc = JsonConvert.DeserializeObject<CertResponse>(str);
                if (acc != null && acc.Count > 0)
                {
                    return acc.Items.ElementAt(0);
                }
            }

            return null;
        }

        /// <summary>
        /// Lấy thông tin tổ chức/ doanh nghiệp của tài khoản.
        /// Thanh toán giao dịch thông qua gói cước tổ chức/ doanh nghiệp đã mua trước đó
        /// </summary>
        /// <param name="access_token"></param>
        /// <returns></returns>
        private static string _getGroupId(string groupAdminEmail, string access_token)
        {
            var response = CoreServiceClient.Query(new RequestMessage
            {
                RequestID = Guid.NewGuid().ToString(),
                ServiceID = "UserAccount",
                FunctionName = "GetProfile"
            }, access_token);
            if (response != null)
            {
                var str = JsonConvert.SerializeObject(response.Content);
                Account acc = JsonConvert.DeserializeObject<Account>(str);
                if (acc != null && acc.Groups != null)
                {
                    foreach (var group in acc.Groups)
                    {
                        if (groupAdminEmail.Equals(group.AdminEmail))
                        {
                            // TODO: Có thể bổ sung kiểm tra lượt ký còn lại, ngày hết hạn gói cước ở đây.
                            return group.ID;
                        }
                    }
                }
            }

            return null;
        }


        // Sample pdf input file
        private const string _pdfInput = @"F:/WORK/2019/01-2019/input.pdf";
        private const string _pdfSignedPath = @"F:/WORK/2019/01-2019/signed.pdf";
        private const string _certBase64 = "MIIFeDCCA2CgAwIBAgIQVAEBARDjGFDeQ9Qub1PFMTANBgkqhkiG9w0BAQUFADBpMQswCQYDVQQGEwJWTjETMBEGA1UEChMKVk5QVCBHcm91cDEeMBwGA1UECxMVVk5QVC1DQSBUcnVzdCBOZXR3b3JrMSUwIwYDVQQDExxWTlBUIENlcnRpZmljYXRpb24gQXV0aG9yaXR5MB4XDTE4MDMxNjEwMDU0OFoXDTE5MTIxNjA2NTgwOFowgbkxCzAJBgNVBAYTAlZOMRIwEAYDVQQIDAlIw6AgTuG7mWkxFTATBgNVBAcMDEPhuqd1IEdp4bqleTEWMBQGA1UECgwNVk5QVCBTb2Z0d2FyZTEaMBgGA1UECwwRQ2h1bmcgdGh1IGRpZW4gdHUxSzBJBgNVBAMMQkPDlE5HIFRZIFBI4bqmTiBN4buATSBWTlBULUNISSBOSEFOSCBU4buUTkcgQ8OUTkcgVFkgVknhu4ROIFRIw5RORzCBnzANBgkqhkiG9w0BAQEFAAOBjQAwgYkCgYEAkLHKRYjfPBoQlYCwpus9vlzmwCsS1n9MdpH326OPYWVo5UEy6RLKlQgQqjcKfju3KsZoYfle5Hhro3HGDzkONb7HDRTz4ScTbKosExyAYqeoWIt4vT9BoUiSoo8w8pCKLZpgHbnZ0MYh5I16Qv7fukSAS8y9bp/F2POwhM0P2MECAwEAAaOCAU0wggFJMDwGCCsGAQUFBwEBBDAwLjAsBggrBgEFBQcwAYYgaHR0cDovL29jc3Audm5wdC1jYS52bi9yZXNwb25kZXIwHQYDVR0OBBYEFBCZA5DOC7IF8B580OCDo5/emLm9MAkGA1UdEwQCMAAwHwYDVR0jBBgwFoAUBmnA1dUCihWNRn3pfOJoClWsaq8waAYDVR0gBGEwXzBdBgwrBgEEAYHtAwEBAwMwTTAkBggrBgEFBQcCAjAYHhYAQwBTAC0AQgAxAC4AMAAtADEAMgBtMCUGCCsGAQUFBwIBFhlodHRwOi8vcHViLnZucHQtY2Eudm4vcnBhMDEGA1UdHwQqMCgwJqAkoCKGIGh0dHA6Ly9jcmwudm5wdC1jYS52bi92bnB0Y2EuY3JsMAwGA1UdDwQFAwMH0YAwEwYDVR0lBAwwCgYIKwYBBQUHAwMwDQYJKoZIhvcNAQEFBQADggIBALwxj9atXdZvinfXwAV9mClG74VWzZe6Bdyu4kyfuYuJrPe6COSSD8KW46oMcUHKtBP6tceYMeWsyPCdFFfMh4qWuGyEDbQk+MPGjbodf/bPLwcr/FIM5D23s1LPKIn6htpVHu0xE4apOEPYnKiuvtjx9jN82w+krRoJ1MxUFfeWfpifVv43NsMBnKkL3JKVmBZ0rUL6RFpZwu7Rfb4XOi0umBmYxa/x9Qclkn9kQRc6SR/UlIG7449XI/NDqSevT4zoRMviRhoOB+gBn3D6e2EQMLsFao8MzjXGya8abAw0tHFnb/BvstI2I/PkYISrMjqDJWEWQzSXmak7Jc31aJMB0qA+D+HZSDi17MFq4pGtwqlD9ta12235l4W/dFlScUohY4GCqYayl9GrpHT6qW735LD3SXrt7Iv5wfyPK8eMbsCA5ZTXp8ygC0eF9tGW5GWQdC/vuTxE+WcKY9RBBQEB09dlruvqa72FhZyuyGRnp4r4okL4/1xwf1Q5QQTOqglUv/ehmWlyg2j83ABhIpxROZ+ZvYt2Ldg36ab3wwCLjo69JEcjQzn8APODF1CcfXfmIyyZiT8q9/uhXEqCR6joKldDHvTUIR4LeaLhH1RrlfOv1Fwg8L63ZngQ63Z/FhvdCksXNFuNyGhJg4eA0TmOK9JrSUFpWO29A1eiRjJW";
        private static void _hashSignSample()
        {
            _log.Info("SignHash PDF: Starting...");

            // 1. Get second hash value from unsigned data ---------------------------------------------------
            byte[] unsignData = null;
            try
            {
                unsignData = File.ReadAllBytes(_pdfInput);
            }
            catch (Exception ex)
            {
                _log.Error(ex);
                return;
            }
            var tsaUrl = "http://timestamp.globalsign.com/scripts/timstamp.dll";
            IHashSigner signer = HashSignerFactory.GenerateSigner(unsignData, _certBase64, tsaUrl, HashSignerFactory.PDF);

            #region Optional -----------------------------------
            // Signature reason
            ((PdfHashSigner)signer).SetReason("Sign reason");
            // Signature custom image
            var image = "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";
            ((PdfHashSigner)signer).SetCustomImage(Convert.FromBase64String(image));
            // Signing page
            ((PdfHashSigner)signer).SetSigningPage(1);
            ((PdfHashSigner)signer).SetSignaturePosition(50, 50, 250, 100);
            #endregion -----------------------------------------

            var hashValue = signer.GetSecondHashAsBase64();
            Console.WriteLine(hashValue);
            // ------------------------------------------------------------------------------------------

            // 2. Sign hashvalue using service api ------------------------------------------------------
            var externalSignature = "E2W87bTgFH6MyhjBXs7X5Mn78x2ksCZKYKr04Nwult1mP6t+AzuoreIJWYFdV0diE2FCs09FTEXS/vI9ikE7xaRoK9dv0O7rgfdFI4t56EllLrLj4UdNhkfyyP27MjFeJbByJgLEUQDNj3Q3wCAe0OaFqNUr7T8sAUvD6WhKvo0=";
            if (string.IsNullOrEmpty(externalSignature))
            {
                _log.Error("Sign error");
                return;
            }

            if (!signer.CheckHashSignature(externalSignature))
            {
                _log.Error("Signature not match");
                return;
            }
            // ------------------------------------------------------------------------------------------

            // 3. Package external signature to signed file
            byte[] signed = signer.Sign(externalSignature);
            var x = Convert.ToBase64String(signed);

            File.WriteAllBytes(_pdfSignedPath, signed);
            _log.Info("SignHash PDF: Successfull. signed file at '" + _pdfSignedPath + "'");
        }


    }

    /*
    private Task CompleteTran(IList<IHashSigner> signers, Guid tranId, uint chalId)
    {
        Task.Run(async () =>
        {
            for (int i = 0; i < 300; i++)
            {
                _logger.LogInformation("Waiting user confirm...");
                SignatureTransaction tran = await _signatureRepo.FindAsync(tranId);
                if (tran.Status != SignatureTransaction.Statuses.WAITING_FOR_SIGNER_CONFIRM)
                {
                    if (tran.Status == SignatureTransaction.Statuses.SUCCESS)
                    {
                        _logger.LogInformation($"Complete tran {tranId}");
                        for (int j = 0; j < signers.Count; j++)
                        {
                            var signer = signers[j];
                            var sig = tran.Documents[j].Signature;
                            if (signer.CheckHashSignature(sig))
                            {
                                _logger.LogInformation($"Signature valid");
                                tran.Documents[j].DataSigned = signer.Sign(sig);
                                //System.IO.File.WriteAllBytes(@$"C:\RemoteSigning{j}_signed.pdf", tran.Documents[j].DataSigned);
                                await _signatureRepo.Update(tran, tran.Id);
                                if (tran.TranType == SignatureTransaction.TranTypes.ACCEPTANCE)
                                {
                                    try
                                    {
                                        BackgroundJob.Enqueue<IRequestCertificateJob>(c => c.SyncAcceptance(null, tran.Id.ToString()));
                                    }
                                    catch (Exception) { }
                                }
                                _logger.LogInformation("Sign successful!");
                            }
                            else
                            {
                                tran.Status = SignatureTransaction.Statuses.SIGN_FAILED;
                                tran.StatusDetail = "Signature invalid";
                                _logger.LogError("Signature invalid");
                            }
                        }
                    }
                    return;
                }
                if (tran.ChallengeId != chalId)
                {
                    return;
                }
                //_logger.LogInformation("{0}", signer.GetSignerSubjectDN());
                Thread.Sleep(1000);
            }
        });
        return Task.CompletedTask;
    }
    */
    public class MyXmlDsigExcC14NTransform : XmlDsigExcC14NTransform
    {
        public MyXmlDsigExcC14NTransform() { }

        public override void LoadInput(Object obj)
        {
            XmlElement root = ((XmlDocument)obj).DocumentElement;
            if (root.Name == "SignedInfo") root.RemoveAttribute("xml:id");
            base.LoadInput(obj);
        }
    }
}
