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
using System.Windows.Forms.VisualStyles;
using System.Windows.Forms;
using System.Drawing;

namespace testSigning_Winform
{
    public class RemoteSign
    {
        private string SignedFolderPath = "C:\\Users\\quanna\\Desktop\\testapi_smartca\\TestResult";

        public string uid { get; set; }

        private string client_id;
        public string Client_Id
        {
            get
            {
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
        public readonly string GuidHS;

        private frmRemoteSign frm;
        public RemoteSign(frmRemoteSign frm)
        {
            this.frm = frm;
            GuidHS = Guid.NewGuid().ToString();
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
            int count = 0;
            foreach (FileInfo fi in listFiles)
            {
                count++;
                FileDisplayControl fileControl = new FileDisplayControl(fi, count);
                fileControl.SetNameControl(fi.FullName);
                frm.panelToKhai.Controls.Add(fileControl);
            }
        }

        public void SignToKhai(FileDisplayControl[] fileControls)
        {
            //lay cert
            UserCertificate userCert = GetCertificate();
            if(userCert == null)
            {
                MessageBox.Show("User not found", "Error", MessageBoxButtons.OK);
                return;
            }
            foreach (FileDisplayControl control in fileControls)
            {
                DataSign result = null;
                IHashSigner signer = null;
                switch(control.FileDetail.Extension)
                {
                    case ".pdf":
                        result = _signSmartCAPDF(userCert,control.FileDetail.FullName);
                        break;
                    case ".xml":
                        result = _signSmartCAXML(userCert,control.FileDetail.FullName, out signer);

                        break;
                    case ".xlsx":
                    case ".docx":

                        break;
                    default:
                        return;
                }
                if (result == null)
                {
                    // log "Error send file to server VNPT"
                    control.SetStatusText("Failed", Color.Red);
                }
                //co the thay doi thoi gian countdown dua vao data tra ve tu sever, default la 300
                if(signer != null)
                {
                    control.SetSigner(signer);
                }
                control.SetDataSign(result);    
                control.StartCountDown(300);
            }
        }

        public void GetResult_ToKhai(FileDisplayControl[] fileControls, string GuidHS)
        {
            string PathHoso = Path.Combine(SignedFolderPath,GuidHS);
            if (!Directory.Exists(PathHoso))
            {
                Directory.CreateDirectory(PathHoso);
            }

            foreach (FileDisplayControl control in fileControls)
            {
                bool result = false;
                switch (control.FileDetail.Extension)
                {
                    case "pdf":
                        result = GetResult_PDF(control.dataSign, PathHoso);
                        break;
                    case "xml":
                        result = GetResult_Xml(control.signer,control.dataSign,PathHoso);
                        break;
                    case "xlsx":
                    case "docx":

                        break;
                    default:
                        return;
                }
                if (!result) 
                {
                    if (control.CheckTime())
                    {
                        control.SetStatusText("Pending", Color.Yellow);
                        continue;
                    }
                    //log ko lay dc ket qua
                    control.SetStatusText("Failed", Color.Red);
                }
                control.SetSuccess();
            }
        }

        private static string genRandom(int length)
        {
            Random random = new Random();
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            return new string(Enumerable.Repeat(chars, length).Select(s => s[random.Next(s.Length)]).ToArray());
        }

        private UserCertificate GetCertificate()
        {
            var userCert = _getAccountCert("https://rmgateway.vnptit.vn/sca/sp769/v1/credentials/get_certificate");
            //var userCert = _getAccountCert("https://gwsca.vnpt.vn/sca/sp769/v1/credentials/get_certificate");
            if (userCert == null)
            {
                //log Console.WriteLine("not found cert");
                return null;
            }
            if (userCert.Count() == 1)
            {
                return userCert[0];
            }
            if(userCert.Count() > 1)
            {
                //tao 1 form de load cks va chon o day
            }
            //de tam la 0 
            return userCert[0];
        }

        private DataSign _signSmartCAPDF(UserCertificate userCert, string pdfInput)
        {
            try
            {
                byte[] unsignData = null;
                try
                {
                    unsignData = File.ReadAllBytes(pdfInput);
                }
                catch (Exception ex)
                {
                    //_log.Error(ex);
                    return null;
                }
                IHashSigner signer = HashSignerFactory.GenerateSigner(unsignData, userCert.cert_data, null, HashSignerFactory.PDF);
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

                string tempFolder = Path.GetTempPath();
                File.AppendAllText(tempFolder + data_to_be_sign + ".txt", profileJson);

                DataSign dataSign = _sign("https://rmgateway.vnptit.vn/sca/sp769/v1/signatures/sign", data_to_be_sign, userCert.serial_number);

                return dataSign;
            }
            catch (Exception ex)
            {
                //log ex
                return null;
            }
        }

        private bool GetResult_PDF(DataSign dataSign, string PDFSignedPath)
        {
            var isConfirm = false;
            var datasigned = "";
            var mapping = "";
            DataTransaction transactionStatus;
            string tempFolder = Path.GetTempPath();    
            transactionStatus = _getStatus(string.Format("https://rmgateway.vnptit.vn/sca/sp769/v1/signatures/sign/{0}/status", dataSign.transaction_id));
            if (transactionStatus.signatures != null)
            {
                datasigned = transactionStatus.signatures[0].signature_value;
                mapping = transactionStatus.signatures[0].doc_id;
                isConfirm = true;
            }
            else
            {
                //Console.WriteLine(string.Format("Wait for user confirm count : {0}", count));
                //Thread.Sleep(10000);
            }

            if (!isConfirm)
            {
                //log Console.WriteLine(string.Format("Signer not confirm from App"));
                return false;
            }

            if (string.IsNullOrEmpty(datasigned))
            {
                //log Console.WriteLine("Sign error");
                return false;
            }

            string contentTempFile = File.ReadAllText(tempFolder + mapping + ".txt");
            SignerProfile signerProfileNew = JsonConvert.DeserializeObject<SignerProfile>(contentTempFile);
            var signer1 = HashSignerFactory.GenerateSigner(signerProfileNew.DocType);
            if (!signer1.CheckHashSignature(signerProfileNew, datasigned))
            {
                //log Console.WriteLine("Signature not match");
                return false;
            }
            // ------------------------------------------------------------------------------------------

            // 3. Package external signature to signed file
            byte[] signed = signer1.Sign(signerProfileNew, datasigned);

            File.WriteAllBytes(PDFSignedPath, signed);
            return true;
        }


        private DataSign _signSmartCAXML(UserCertificate userCert ,string xmlInput, out IHashSigner signer)
        {
            try
            {
                String certBase64 = userCert.cert_data;
                byte[] unsignData = null;
                try
                {
                    unsignData = File.ReadAllBytes(xmlInput);
                }
                catch (Exception ex)
                {
                    //_log.Error(ex);
                    signer = null; 
                    return null;
                }
                signer = HashSignerFactory.GenerateSigner(unsignData, certBase64, null, HashSignerFactory.XML);
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

                //Console.WriteLine(string.Format("Wait for user confirm: Transaction_id = {0}", dataSign.transaction_id));
                //Console.ReadKey();
                return dataSign;

            }
            catch (Exception ex)
            {
                //log ex
                signer = null;
                return null;
            }  
        }

        private bool GetResult_Xml(IHashSigner signer, DataSign dataSign, string xmlSignedPath)
        {
            var isConfirm = false;
            var datasigned = "";
            DataTransaction transactionStatus;


            transactionStatus = _getStatus(string.Format("https://rmgateway.vnptit.vn/sca/sp769/v1/signatures/sign/{0}/status", dataSign.transaction_id));
            if (transactionStatus.signatures != null)
            {
                datasigned = transactionStatus.signatures[0].signature_value;
                isConfirm = true;
            }
            else
            {
                
            }

            if (!isConfirm)
            {
                //Console.WriteLine(string.Format("Signer not confirm from App"));
                return false;
            }

            if (string.IsNullOrEmpty(datasigned))
            {
                //Console.WriteLine("Sign error");
                return false;
            }

            if (!signer.CheckHashSignature(datasigned))
            {
                //Console.WriteLine("Signature not match");
                return false;
            }

            // ------------------------------------------------------------------------------------------

            // 3. Package external signature to signed file
            byte[] signed = signer.Sign(datasigned);
            File.WriteAllBytes(xmlSignedPath, signed);
            return true;
        }

        private void _signSmartCAOFFICE(UserCertificate userCert, string officeInput)
        {
            String certBase64 = userCert.cert_data;
            byte[] unsignData = null;
            try
            {
                unsignData = File.ReadAllBytes(officeInput);
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

            
        }

        private bool GetResult_Office(IHashSigner signer, DataSign dataSign, string officeSignedPath)
        {
            var isConfirm = false;
            var datasigned = "";
            DataTransaction transactionStatus;

            
                transactionStatus = _getStatus(string.Format("https://rmgateway.vnptit.vn/sca/sp769/v1/signatures/sign/{0}/status", dataSign.transaction_id));
                if (transactionStatus.signatures != null)
                {
                    datasigned = transactionStatus.signatures[0].signature_value;
                    isConfirm = true;
                }
                else
                {
                   
                }
            if (!isConfirm)
            {
                //log Console.WriteLine(string.Format("Signer not confirm from App"));
                return false;
            }

            if (string.IsNullOrEmpty(datasigned))
            {
                //log Console.WriteLine("Sign error");
                return false;
                }

            // ------------------------------------------------------------------------------------------

            // 3. Package external signature to signed file
            byte[] signed = signer.Sign(datasigned);
            File.WriteAllBytes(officeSignedPath, signed);
            return true;
        }

        private UserCertificate[] _getAccountCert(String uri, string serialNumber = "")
        {
            try
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

                    if (serialNumber == "")
                    {
                        return res.data.user_certificates.ToArray();
                    }

                    var cert = res.data.user_certificates.Where(c => c.serial_number == serialNumber);
                    return cert.ToArray();

                }
                return null;
            }
            catch (Exception)
            {
                //log
                return null;
            }
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
