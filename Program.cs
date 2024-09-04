using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Security;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RestSharp;
using System.IO;
using VnptHashSignatures.Common;
using VnptHashSignatures.Interface;
using VnptHashSignatures.Pdf;
using System.Threading;
using VnptHashSignatures.Xml;
using System.Security.Cryptography.X509Certificates;
using System.Diagnostics;
using Org.BouncyCastle.Ocsp;

namespace SmartCA769
{
    class Program
    {

        private static string client_id = "4184-637127995547330633.apps.signserviceapi.com";
        private static string client_secret = "NGNhMzdmOGE-OGM2Mi00MTg0";
        private static string uid = "036092008610";        
        

        private static string _pdfInput = @"C:\Users\accca\Desktop\test.pdf";
        private static string _pdfSignedPath = @"C:\Users\accca\Desktop\test_signed.pdf";

        private static string _xmlInput = @"C:\Users\accca\Desktop\test.xml";
        private static string _xmlSignedPath = @"C:\Users\accca\Desktop\test_signed.xml";

        private static string _officeInput = @"C:\Users\accca\Desktop\test.docx";
        private static string _officeSignedPath = @"C:\Users\accca\Desktop\test_signed.docx";

        private static string _cmsInput = @"C:\Users\accca\Desktop\test.txt";
        private static string _cmsSignedPath = @"C:\Users\accca\Desktop\test_signed.txt";

        static void Main(string[] args)
        {

            _signSmartCAPDF();
            //_signSmartCAXML();
            //_signSmartCAOFFICE();            
            Console.ReadKey();
        }

        private static void _signSmartCAPDF()
        {
            var userCert = _getAccountCert("https://rmgateway.vnptit.vn/sca/sp769/v1/credentials/get_certificate");
            //var userCert = _getAccountCert("https://gwsca.vnpt.vn/sca/sp769/v1/credentials/get_certificate");
            if (userCert == null)
            {
                Console.WriteLine("not found cert");
                return;
            }
            String certBase64 = userCert.cert_data;


            byte[] unsignData = null;
            try
            {
                unsignData = File.ReadAllBytes(_pdfInput);
            }
            catch (Exception ex)
            {
                //_log.Error(ex);
                return;
            }
            IHashSigner signer = HashSignerFactory.GenerateSigner(unsignData, certBase64, null, HashSignerFactory.PDF);
            signer.SetHashAlgorithm(MessageDigestAlgorithm.SHA256);

            #region Optional -----------------------------------
            // Property: Lý do ký số
            ((PdfHashSigner)signer).SetReason("Xác nhận tài liệu");
            // Hình ảnh hiển thị trên chữ ký (mặc định là logo VNPT)
            //var imgBytes = File.ReadAllBytes(@"C:\Users\Hung Vu\Desktop\aaaa.jpg");
            //var x = Convert.ToBase64String(imgBytes);
            //((PdfHashSigner)signer).SetCustomImage(imgBytes);
            // Signing page (@deprecated)
            //((PdfHashSigner)signer).SetSigningPage(1);
            // Vị trí và kích thước chữ ký (@deprecated)
            //((PdfHashSigner)signer).SetSignaturePosition(20, 20, 220, 50);
            // Kiểu hiển thị chữ ký (OPTIONAL/DEFAULT=TEXT_WITH_BACKGROUND)
            ((PdfHashSigner)signer).SetRenderingMode(PdfHashSigner.RenderMode.TEXT_ONLY);
            // Nội dung text trên chữ ký (OPTIONAL)
            ((PdfHashSigner)signer).SetLayer2Text("Ngày ký: 15/03/2022 \n Người ký: Ngô Quang Đạt \n Nơi ký: VNPT-IT");
            // Fontsize cho text trên chữ ký (OPTIONAL/DEFAULT = 10)
            ((PdfHashSigner)signer).SetFontSize(10);
            //((PdfHashSigner)signer).SetLayer2Text("yahooooooooooooooooooooooooooo");
            // Màu text trên chữ ký (OPTIONAL/DEFAULT=000000)
            ((PdfHashSigner)signer).SetFontColor("0000ff");
            // Kiểu chữ trên chữ ký
            ((PdfHashSigner)signer).SetFontStyle(PdfHashSigner.FontStyle.Normal);
            // Font chữ trên chữ ký
            ((PdfHashSigner)signer).SetFontName(PdfHashSigner.FontName.Arial);

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
                Rectangle = "10,10,250,100",

                Page = 1
            });

            //((PdfHashSigner)signer).AddSignatureView(new PdfSignatureView
            //{
            //    Rectangle = "283,677,404,755", //"283,677,404,755", //283,564,404,642
            //    Page = 2
            //});

            //((PdfHashSigner)signer).AddSignatureComment(new PdfSignatureComment
            //{
            //    Type = (int)PdfSignatureComment.Types.TEXT,
            //    Text = "This is comment",
            //    Page = 1,
            //    Rectangle = "20,20,220,50",
            //});

            // Thêm comment vào dữ liệu

            //((PdfHashSigner)signer).AddSignatureComment(new PdfSignatureComment
            //{
            //    Page = 1,
            //    Rectangle = "348,19,601,95",
            //    Background = "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",
            //    Type = (int)PdfSignatureComment.Types.IMAGE,

            //});
            #endregion -----------------------------------------            

            var profile = signer.GetSignerProfile();

            var profileJson = JsonConvert.SerializeObject(profile);

            var hashValue = Convert.ToBase64String(profile.SecondHashBytes);

            var data_to_be_sign = BitConverter.ToString(Convert.FromBase64String(hashValue)).Replace("-", "").ToLower();

            string tempFolder = Path.GetTempPath();
            File.AppendAllText(tempFolder + data_to_be_sign + ".txt", profileJson);

            DataSign dataSign = _sign("https://rmgateway.vnptit.vn/sca/sp769/v1/signatures/sign", data_to_be_sign, userCert.serial_number);

            Console.WriteLine(string.Format("Wait for user confirm: Transaction_id = {0}", dataSign.transaction_id));
            //Console.ReadKey();

            var count = 0;
            var isConfirm = false;
            var datasigned = "";
            var mapping = "";
            DataTransaction transactionStatus;

            while (count < 30 && !isConfirm)
            {
                transactionStatus = _getStatus(string.Format("https://rmgateway.vnptit.vn/sca/sp769/v1/signatures/sign/{0}/status", dataSign.transaction_id));
                if (transactionStatus.signatures != null)
                {
                    datasigned = transactionStatus.signatures[0].signature_value;
                    mapping = transactionStatus.signatures[0].doc_id;
                    isConfirm = true;
                }
                else
                {
                    count = count + 1;
                    Console.WriteLine(string.Format("Wait for user confirm count : {0}", count));
                    Thread.Sleep(10000);
                }
            }
            if (!isConfirm)
            {
                Console.WriteLine(string.Format("Signer not confirm from App"));
                return;
            }

            if (string.IsNullOrEmpty(datasigned))
            {
                Console.WriteLine("Sign error");
                return;
            }

            string contentTempFile = File.ReadAllText(tempFolder + mapping + ".txt");
            SignerProfile signerProfileNew = JsonConvert.DeserializeObject<SignerProfile>(contentTempFile);
            var signer1 = HashSignerFactory.GenerateSigner(signerProfileNew.DocType);
            if (!signer1.CheckHashSignature(signerProfileNew, datasigned))
            {
                Console.WriteLine("Signature not match");
                return;
            }
            // ------------------------------------------------------------------------------------------

            // 3. Package external signature to signed file
            byte[] signed = signer1.Sign(signerProfileNew, datasigned);

            File.WriteAllBytes(_pdfSignedPath, signed);

        }

        private static void _signSmartCAPDFNotAsync()
        {
            var userCert = _getAccountCert("https://rmgateway.vnptit.vn/sca/sp769/v1/credentials/get_certificate");
            //var userCert = _getAccountCert("https://gwsca.vnpt.vn/sca/sp769/v1/credentials/get_certificate");
            if (userCert == null)
            {
                Console.WriteLine("not found cert");
                return;
            }
            String certBase64 = userCert.cert_data;            

            byte[] unsignData = null;
            try
            {
                unsignData = File.ReadAllBytes(_pdfInput);
            }
            catch (Exception ex)
            {
                //_log.Error(ex);
                return;
            }
            IHashSigner signer = HashSignerFactory.GenerateSigner(unsignData, certBase64, null, HashSignerFactory.PDF);
            signer.SetHashAlgorithm(MessageDigestAlgorithm.SHA256);

            #region Optional -----------------------------------
            // Property: Lý do ký số
            ((PdfHashSigner)signer).SetReason("Xác nhận tài liệu");
            // Hình ảnh hiển thị trên chữ ký (mặc định là logo VNPT)
            //var imgBytes = File.ReadAllBytes(@"C:\Users\Hung Vu\Desktop\aaaa.jpg");
            //var x = Convert.ToBase64String(imgBytes);
            //((PdfHashSigner)signer).SetCustomImage(imgBytes);
            // Signing page (@deprecated)
            //((PdfHashSigner)signer).SetSigningPage(1);
            // Vị trí và kích thước chữ ký (@deprecated)
            //((PdfHashSigner)signer).SetSignaturePosition(20, 20, 220, 50);
            // Kiểu hiển thị chữ ký (OPTIONAL/DEFAULT=TEXT_WITH_BACKGROUND)
            ((PdfHashSigner)signer).SetRenderingMode(PdfHashSigner.RenderMode.TEXT_ONLY);
            // Nội dung text trên chữ ký (OPTIONAL)
            ((PdfHashSigner)signer).SetLayer2Text("Ngày ký: 15/03/2022 \n Người ký: Ngô Quang Đạt \n Nơi ký: VNPT-IT");
            // Fontsize cho text trên chữ ký (OPTIONAL/DEFAULT = 10)
            ((PdfHashSigner)signer).SetFontSize(10);
            //((PdfHashSigner)signer).SetLayer2Text("yahooooooooooooooooooooooooooo");
            // Màu text trên chữ ký (OPTIONAL/DEFAULT=000000)
            ((PdfHashSigner)signer).SetFontColor("0000ff");
            // Kiểu chữ trên chữ ký
            ((PdfHashSigner)signer).SetFontStyle(PdfHashSigner.FontStyle.Normal);
            // Font chữ trên chữ ký
            ((PdfHashSigner)signer).SetFontName(PdfHashSigner.FontName.Arial);

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
                Rectangle = "10,10,250,100",

                Page = 1
            });

            //((PdfHashSigner)signer).AddSignatureView(new PdfSignatureView
            //{
            //    Rectangle = "283,677,404,755", //"283,677,404,755", //283,564,404,642
            //    Page = 2
            //});

            //((PdfHashSigner)signer).AddSignatureComment(new PdfSignatureComment
            //{
            //    Type = (int)PdfSignatureComment.Types.TEXT,
            //    Text = "This is comment",
            //    Page = 1,
            //    Rectangle = "20,20,220,50",
            //});

            // Thêm comment vào dữ liệu

            //((PdfHashSigner)signer).AddSignatureComment(new PdfSignatureComment
            //{
            //    Page = 1,
            //    Rectangle = "348,19,601,95",
            //    Background = "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",
            //    Type = (int)PdfSignatureComment.Types.IMAGE,

            //});
            #endregion -----------------------------------------            

            var profile = signer.GetSignerProfile();

            var profileJson = JsonConvert.SerializeObject(profile);

            var hashValue = Convert.ToBase64String(profile.SecondHashBytes);            

            var data_to_be_sign = BitConverter.ToString(Convert.FromBase64String(hashValue)).Replace("-", "").ToLower();

            string tempFolder = Path.GetTempPath();
            File.AppendAllText(tempFolder+ data_to_be_sign + ".txt", profileJson);

            DataSign dataSign = _sign("https://rmgateway.vnptit.vn/sca/sp769/v1/signatures/sign", data_to_be_sign, userCert.serial_number);

            Console.WriteLine(string.Format("Wait for user confirm: Transaction_id = {0}", dataSign.transaction_id));
            //Console.ReadKey();

            var count = 0;
            var isConfirm = false;
            var datasigned = "";
            var mapping  = "";
            DataTransaction transactionStatus;
            
            while (count < 30 && !isConfirm)
            {
                transactionStatus = _getStatus(string.Format("https://rmgateway.vnptit.vn/sca/sp769/v1/signatures/sign/{0}/status", dataSign.transaction_id));
                if (transactionStatus.signatures != null)
                {
                    datasigned = transactionStatus.signatures[0].signature_value;
                    mapping = transactionStatus.signatures[0].doc_id;
                    isConfirm = true;
                }
                else
                {
                    count = count + 1;
                    Console.WriteLine(string.Format("Wait for user confirm count : {0}", count));
                    Thread.Sleep(10000);
                }
            }
            if (!isConfirm)
            {
                Console.WriteLine(string.Format("Signer not confirm from App"));
                return;
            }

            if (string.IsNullOrEmpty(datasigned))
            {
                Console.WriteLine("Sign error");
                return;
            }
            
            string contentTempFile = File.ReadAllText(tempFolder + mapping + ".txt");
            SignerProfile signerProfileNew = JsonConvert.DeserializeObject<SignerProfile>(contentTempFile);
            var signer1 = HashSignerFactory.GenerateSigner(signerProfileNew.DocType);
            if (!signer1.CheckHashSignature(signerProfileNew, datasigned))
            {
                Console.WriteLine("Signature not match");
                return;
            }
            // ------------------------------------------------------------------------------------------

            // 3. Package external signature to signed file
            byte[] signed = signer1.Sign(signerProfileNew,datasigned);
            
            File.WriteAllBytes(_pdfSignedPath, signed);

        }


        private static void _signSmartCAXML()
        {
            var userCert = _getAccountCert("https://rmgateway.vnptit.vn/sca/sp769/v1/credentials/get_certificate");
            //var userCert = _getAccountCert("https://gwsca.vnpt.vn/sca/sp769/v1/credentials/get_certificate");
            if (userCert == null)
            {
                Console.WriteLine("not found cert");
                return;
            }
            String certBase64 = userCert.cert_data;


            byte[] unsignData = null;
            try
            {
                unsignData = File.ReadAllBytes(_xmlInput);
            }
            catch (Exception ex)
            {
                //_log.Error(ex);
                return;
            }
            IHashSigner signer = HashSignerFactory.GenerateSigner(unsignData, certBase64, null, HashSignerFactory.XML);
            signer.SetHashAlgorithm(MessageDigestAlgorithm.SHA256);

            //Set ID cho thẻ ssignature
            ((XmlHashSigner)signer).SetSignatureID(Guid.NewGuid().ToString());

            //Set reference đến id
            //((XmlHashSigner)signers).SetReferenceId("#SigningData");

            //Set thời gian ký
            ((XmlHashSigner)signer).SetSigningTime(DateTime.Now, "SigningTime-" + Guid.NewGuid().ToString());

            //đường dẫn dẫn đến thẻ chứa chữ ký 
            ((XmlHashSigner)signer).SetParentNodePath("/TDiep/DLieu/HDon/DSCKS/NBan");


            var hashValue = signer.GetSecondHashAsBase64();

            var data_to_be_sign = BitConverter.ToString(Convert.FromBase64String(hashValue)).Replace("-", "").ToLower();

            DataSign dataSign = _sign("https://rmgateway.vnptit.vn/sca/sp769/v1/signatures/sign", data_to_be_sign, userCert.serial_number);

            Console.WriteLine(string.Format("Wait for user confirm: Transaction_id = {0}", dataSign.transaction_id));
            //Console.ReadKey();

            var count = 0;
            var isConfirm = false;
            var datasigned = "";
            DataTransaction transactionStatus;

            while (count < 30 && !isConfirm)
            {
                transactionStatus = _getStatus(string.Format("https://rmgateway.vnptit.vn/sca/sp769/v1/signatures/sign/{0}/status", dataSign.transaction_id));
                if (transactionStatus.signatures != null)
                {
                    datasigned = transactionStatus.signatures[0].signature_value;
                    isConfirm = true;
                }
                else
                {
                    count = count + 1;
                    Console.WriteLine(string.Format("Wait for user confirm count : {0}", count));
                    Thread.Sleep(10000);
                }
            }
            if (!isConfirm)
            {
                Console.WriteLine(string.Format("Signer not confirm from App"));
                return;
            }

            if (string.IsNullOrEmpty(datasigned))
            {
                Console.WriteLine("Sign error");
                return;
            }

            if (!signer.CheckHashSignature(datasigned))
            {
                Console.WriteLine("Signature not match");
                return;
            }

            // ------------------------------------------------------------------------------------------

            // 3. Package external signature to signed file
            byte[] signed = signer.Sign(datasigned);
            File.WriteAllBytes(_xmlSignedPath, signed);

        }

        private static void _signSmartCAOFFICE()
        {
            var userCert = _getAccountCert("https://rmgateway.vnptit.vn/sca/sp769/v1/credentials/get_certificate");
            //var userCert = _getAccountCert("https://gwsca.vnpt.vn/sca/sp769/v1/credentials/get_certificate");
            if (userCert == null)
            {
                Console.WriteLine("not found cert");
                return;
            }
            String certBase64 = userCert.cert_data;


            byte[] unsignData = null;
            try
            {
                unsignData = File.ReadAllBytes(_officeInput);
            }
            catch (Exception ex)
            {
                //_log.Error(ex);
                return;
            }
            IHashSigner signer = HashSignerFactory.GenerateSigner(unsignData, certBase64, null, HashSignerFactory.OFFICE);
            signer.SetHashAlgorithm(MessageDigestAlgorithm.SHA256);            


            var hashValue = signer.GetSecondHashAsBase64();

            var data_to_be_sign = BitConverter.ToString(Convert.FromBase64String(hashValue)).Replace("-", "").ToLower();

            DataSign dataSign = _sign("https://rmgateway.vnptit.vn/sca/sp769/v1/signatures/sign", data_to_be_sign, userCert.serial_number);

            Console.WriteLine(string.Format("Wait for user confirm: Transaction_id = {0}", dataSign.transaction_id));
            //Console.ReadKey();

            var count = 0;
            var isConfirm = false;
            var datasigned = "";
            DataTransaction transactionStatus;

            while (count < 30 && !isConfirm)
            {
                transactionStatus = _getStatus(string.Format("https://rmgateway.vnptit.vn/sca/sp769/v1/signatures/sign/{0}/status", dataSign.transaction_id));
                if (transactionStatus.signatures != null)
                {
                    datasigned = transactionStatus.signatures[0].signature_value;
                    isConfirm = true;
                }
                else
                {
                    count = count + 1;
                    Console.WriteLine(string.Format("Wait for user confirm count : {0}", count));
                    Thread.Sleep(10000);
                }
            }
            if (!isConfirm)
            {
                Console.WriteLine(string.Format("Signer not confirm from App"));
                return;
            }

            if (string.IsNullOrEmpty(datasigned))
            {
                Console.WriteLine("Sign error");
                return;
            }            

            // ------------------------------------------------------------------------------------------

            // 3. Package external signature to signed file
            byte[] signed = signer.Sign(datasigned);
            File.WriteAllBytes(_officeSignedPath, signed);

        }

        private static UserCertificate _getAccountCert(String uri)
        {
            var response = Query(new ReqGetCert
            {
                sp_id = client_id,
                sp_password = client_secret,
                user_id = uid,
                serial_number = "",
                transaction_id = "321"
            }, uri);
            if (response != null)
            {
                ResGetCert res = JsonConvert.DeserializeObject<ResGetCert>(response);

                if (res.data.user_certificates.Count() == 1)
                {
                    return res.data.user_certificates[0];
                }
                else if (res.data.user_certificates.Count() > 1)
                {
                    for (int i = 0; i < res.data.user_certificates.Count(); i++)
                    {
                        Console.WriteLine("--------------");
                        Console.WriteLine("Certificate index : " + i);
                        Console.WriteLine("service_type : " + res.data.user_certificates[i].service_type);
                        Console.WriteLine("service_name : " + res.data.user_certificates[i].service_name);
                        Console.WriteLine("serial_number : " + res.data.user_certificates[i].serial_number);
                        Console.WriteLine("cert_subject : " + res.data.user_certificates[i].cert_subject);
                        Console.WriteLine("cert_valid_from : " + res.data.user_certificates[i].cert_valid_from);
                        Console.WriteLine("cert_valid_to : " + res.data.user_certificates[i].cert_valid_to);
                    }
                    Console.WriteLine("Choose Certificate index :");
                    String certIndex = Console.ReadLine();
                    int certIn;
                    bool isNumber = int.TryParse(certIndex, out certIn);
                    if (!isNumber)
                    {                        
                        return null; ;
                    }
                    if (certIn < 0 || certIn >= res.data.user_certificates.Count())
                    {                        
                        return null;
                    }
                    return res.data.user_certificates[certIn];

                }
                else
                {                    
                    return null;
                }
                
            }
            return null;

        }

        private static DataSign _sign(String uri, string data_to_be_signed, String serialNumber)
        {

            
            var sign_files = new List<SignFile>();
            var sign_file = new SignFile();
            sign_file.data_to_be_signed = data_to_be_signed;
            sign_file.doc_id = data_to_be_signed;
            sign_file.file_type = "pdf";
            sign_file.sign_type = "hash";
            sign_files.Add(sign_file);
            var response = Query(new ReqSign
            {
                sp_id = client_id,
                sp_password = client_secret,
                user_id = uid,                
                transaction_id = Guid.NewGuid().ToString(),
                transaction_desc  = "Ký Test từ NgoQuangDat",
                sign_files = sign_files,
                serial_number = serialNumber,

            }, uri);
            if (response != null)
            {
                ResSign req = JsonConvert.DeserializeObject<ResSign>(response);
                return req.data;
            }
            return null;
        }


        private static String Query(object req, string serviceUri)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            ServicePointManager.ServerCertificateValidationCallback
                += new RemoteCertificateValidationCallback(SslHelper.ValidateRemoteCertificate);

            RestClient client = new RestClient(serviceUri);
            RestRequest request = new RestRequest(Method.POST);
            request.AddHeader("Content-Type", "application/json");
            var body = JsonConvert.SerializeObject(req);
            request.AddParameter("application/json", body, ParameterType.RequestBody);
            IRestResponse response = null;
            try
            {
                response = client.Execute(request);
            }
            catch (Exception ex)
            {
                //_log.Error($"Connect gateway error: {ex.Message}", ex);
                return null;
            }

            if (response == null || response.ErrorException != null)
            {
                //_log.Error("Service return null response");
                return null;
            }
            if (response.StatusCode != HttpStatusCode.OK)
            {
                //_log.Error($"Status code={response.StatusCode}. Status content: {response.Content}");
                return null;
            }

            return response.Content;
        }

        private static DataTransaction _getStatus(String uri)
        {
            var response = Query(new Object
            {                
            }, uri);
            if (response != null)
            {
                ResStatus res = JsonConvert.DeserializeObject<ResStatus>(response);
                return res.data;
            }
            return null;
        }
    }
    public class ResStatus
    {
        public int status_code { get; set; }
        public string message { get; set; }
        public DataTransaction data { get; set; }
    }

    public class DataTransaction
    {
        public string transaction_id { get; set; }        
        public List<Signature> signatures { get; set; }
    }

    public class Signature
    {
        public string doc_id { get; set; }
        public string signature_value { get; set; }
        public object timestamp_signature { get; set; }
    }

    public class ReqGetCert
    {
        public string sp_id { get; set; }
        public string sp_password { get; set; }
        public string user_id { get; set; }
        public string serial_number { get; set; }
        public string transaction_id { get; set; }
    }

    public class GetCertData
    {
        public List<UserCertificate> user_certificates { get; set; }
    }
    public class UserCertificate
    {
        public string service_type { get; set; }
        public string service_name { get; set; }
        public string cert_id { get; set; }
        public string cert_status { get; set; }
        public string serial_number { get; set; }
        public string cert_subject { get; set; }
        public DateTime cert_valid_from { get; set; }
        public DateTime cert_valid_to { get; set; }
        public string cert_data { get; set; }
        public ChainData chain_data { get; set; }
        public string transaction_id { get; set; }
    }
    public class ChainData
    {
        public string ca_cert { get; set; }
        public object root_cert { get; set; }
    }

    public class ResGetCert
    {
        public int status_code { get; set; }
        public string message { get; set; }
        public GetCertData data { get; set; }
    }

    public class DataSign
    {
        public string transaction_id { get; set; }
        public string tran_code { get; set; }        
    }
    public class SignFile
    {
        public string data_to_be_signed { get; set; }
        public string doc_id { get; set; }
        public string file_type { get; set; }
        public string sign_type { get; set; }
    }

    public class ReqSign
    {
        public string sp_id { get; set; }
        public string sp_password { get; set; }
        public string user_id { get; set; }
        public string transaction_desc { get; set; }        
        public string transaction_id { get; set; }
        public List<SignFile> sign_files { get; set; }
        public string serial_number { get; set; }
    }

    public class ResSign
    {
        public int status_code { get; set; }
        public string message { get; set; }
        public DataSign data { get; set; }
    }

}
