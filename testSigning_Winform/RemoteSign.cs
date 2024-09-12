using testSigning_Winform.Response;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using testSigning_Winform.CustomControl;
using VnptHashSignatures.Interface;
using VnptHashSignatures.Common;
using VnptHashSignatures.Pdf;
using Newtonsoft.Json;
using System.Threading;
using VnptHashSignatures.Xml;
using testSigning_Winform.Request;
using System.Configuration;
using System.Net;
using RestSharp;

namespace testSigning_Winform
{
    public class RemoteSign
    {
        private string SignedPdfFolderPath = "";
        private string SignedXmlFolderPath = "";
        private string SignedOfficeFolderPath = "";

        public string uid { get; set; }

        private string client_id;
        public string Client_Id
        {
            get {
                if (string.IsNullOrEmpty(client_id))
                {
                    client_id = ConfigurationManager.AppSettings["client_id"];
                }
                return client_id; 
            }
        }

        private string client_password;

        public string Client_Password
        {
            get
            {
                if (string.IsNullOrEmpty(client_password))
                {
                    client_password = ConfigurationManager.AppSettings["client_password"];
                }
                return client_password;
            }
        }


        private frmRemoteSign frm;
        public RemoteSign(frmRemoteSign frm)
        {
            this.frm = frm;
        }

        public void LoadToKhai(string folderPath)
        {
            string[] fileExtensions = { "*.pdf", "*.xml", "*.docx", "*.xlsx" };
            DirectoryInfo di = new DirectoryInfo(folderPath);
            List<FileInfo> listFiles = new List<FileInfo>();
            foreach (string extension in fileExtensions)
            {
               FileInfo[] fi = di.GetFiles(extension, SearchOption.AllDirectories);
                if (fi != null) 
                {
                    listFiles.AddRange(fi); 
                }
            }
            foreach (FileInfo fi in listFiles) 
            {
                FileDisplayControl fileControl = new FileDisplayControl(fi);
                fileControl.SetNameControl(fi.FullName);
                frm.panelToKhai.Controls.Add(fileControl);
            }
        }

        public static void SignToKhai(FileDisplayControl[] fileControls, string guidHS)
        {
            foreach(FileDisplayControl control in fileControls)
            {
                
            }
        }

        private static string genRandom(int length)
        {
            Random random = new Random();
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            return new string(Enumerable.Repeat(chars, length).Select(s => s[random.Next(s.Length)]).ToArray());
        }

        private DataSign _signSmartCAPDF(string pdfInput, out string tempFolder )
        {
            var userCert = _getAccountCert("https://rmgateway.vnptit.vn/sca/sp769/v1/credentials/get_certificate");
            //var userCert = _getAccountCert("https://gwsca.vnpt.vn/sca/sp769/v1/credentials/get_certificate");
            if (userCert == null)
            {
                Console.WriteLine("not found cert");
                tempFolder = "";
                return null;
            }
            String certBase64 = userCert.cert_data;


            byte[] unsignData = null;
            try
            {
                unsignData = File.ReadAllBytes(pdfInput);
            }
            catch (Exception ex)
            {
                //_log.Error(ex);
                tempFolder = "";
                return null;
            }
            IHashSigner signer = HashSignerFactory.GenerateSigner(unsignData, certBase64, null, HashSignerFactory.PDF);
            signer.SetHashAlgorithm(MessageDigestAlgorithm.SHA256);

            #region Optional -----------------------------------
            // Property: Lý do ký số
            ((PdfHashSigner)signer).SetReason("Xác nhận tài liệu");
            // Kiểu hiển thị chữ ký (OPTIONAL/DEFAULT=TEXT_WITH_BACKGROUND)
            ((PdfHashSigner)signer).SetRenderingMode(PdfHashSigner.RenderMode.TEXT_ONLY);
            // Nội dung text trên chữ ký (OPTIONAL)
            ((PdfHashSigner)signer).SetLayer2Text($"Ngày ký: {DateTime.Now.Date} \n Người ký: QuanNa \n Nơi ký: EBH");
            // Fontsize cho text trên chữ ký (OPTIONAL/DEFAULT = 10)
            ((PdfHashSigner)signer).SetFontSize(10);
            //((PdfHashSigner)signer).SetLayer2Text("yahooooooooooooooooooooooooooo");
            // Màu text trên chữ ký (OPTIONAL/DEFAULT=000000)
            ((PdfHashSigner)signer).SetFontColor("0000ff");
            // Kiểu chữ trên chữ ký
            ((PdfHashSigner)signer).SetFontStyle(PdfHashSigner.FontStyle.Normal);
            // Font chữ trên chữ ký
            ((PdfHashSigner)signer).SetFontName(PdfHashSigner.FontName.Arial);

            // Hiển thị ảnh chữ ký tại nhiều vị trí trên tài liệu
            ((PdfHashSigner)signer).AddSignatureView(new PdfSignatureView
            {
                Rectangle = "10,10,250,100",

                Page = 1
            });

            #endregion -----------------------------------------            

            var profile = signer.GetSignerProfile();

            var profileJson = JsonConvert.SerializeObject(profile);

            var hashValue = Convert.ToBase64String(profile.SecondHashBytes);

            var data_to_be_sign = BitConverter.ToString(Convert.FromBase64String(hashValue)).Replace("-", "").ToLower();

            tempFolder = Path.GetTempPath();
            File.AppendAllText(tempFolder + data_to_be_sign + ".txt", profileJson);

            DataSign dataSign = _sign("https://rmgateway.vnptit.vn/sca/sp769/v1/signatures/sign", data_to_be_sign, userCert.serial_number);

           return dataSign;

        }

        private void GetResult(DataSign dataSign, string PDFSignedPath, string tempFolder)
        {
            var count = 0;
            var isConfirm = false;
            var datasigned = "";
            var mapping = "";
            DataTransaction transactionStatus;

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

            File.WriteAllBytes(PDFSignedPath, signed);
        }

        private void _signSmartCAPDFNotAsync()
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
          

            // Hiển thị ảnh chữ ký tại nhiều vị trí trên tài liệu
            ((PdfHashSigner)signer).AddSignatureView(new PdfSignatureView
            {
                Rectangle = "10,10,250,100",

                Page = 1
            });

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


        private void _signSmartCAXML()
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
            //((XmlHashSigner)signer).SetSignatureID(Guid.NewGuid().ToString());
            ((XmlHashSigner)signer).SetSignatureID(Guid.NewGuid().ToString());

            //Set reference đến id
            //((XmlHashSigner)signers).SetReferenceId("#SigningData");

            //Set thời gian ký
            ((XmlHashSigner)signer).SetSigningTime(DateTime.Now, "SigningTime-" + Guid.NewGuid().ToString());

            //đường dẫn dẫn đến thẻ chứa chữ ký 
            ((XmlHashSigner)signer).SetParentNodePath("/Hoso/CKy_Dvi");


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

        private void _signSmartCAOFFICE()
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

        private UserCertificate _getAccountCert(String uri)
        {
            var response = Query(new ReqGetCert
            {
                sp_id = Client_Id,
                sp_password = Client_Password,
                user_id = uid,
                serial_number = "",
                transaction_id = genRandom(5)
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

        private DataSign _sign(String uri, string data_to_be_signed, String serialNumber)
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
                sp_id = Client_Id,
                sp_password = Client_Password,
                user_id = uid,
                transaction_id = Guid.NewGuid().ToString(),
                transaction_desc = "Ký Test từ QuanNguyenAnh",
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

        private String Query(object req, string serviceUri)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            //ServicePointManager.ServerCertificateValidationCallback
            //    += new RemoteCertificateValidationCallback(SslHelper.ValidateRemoteCertificate);

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

        private DataTransaction _getStatus(String uri)
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
}
