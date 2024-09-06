using EBH_RemoteSigning_Service_ERS.CAService;
using EBH_RemoteSigning_Service_ERS.clsUtilities;
using EBH_RemoteSigning_Service_ERS.Request;
using EBH_RemoteSigning_Service_ERS.Response;
using EBH_RemoteSigning_Service_ERS_.Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Web;
using System.Web.Services;
using System.Web.Services.Protocols;
using VnptHashSignatures.Common;
using VnptHashSignatures.Interface;
using VnptHashSignatures.Pdf;

namespace EBH_RemoteSigning_Service_ERS
{
    public class Authorize : SoapHeader
    {
        public int SecretKey { get; set; }
    }

    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    public class RemoteSigningService : System.Web.Services.WebService
    {
        public Authorize AuthorizeHeader;
        private ISmartCAService _smartCAService;
        private readonly DbService _dbService;
        
        public RemoteSigningService()
        {
            ConfigRequest configRequest = new ConfigRequest();
            _smartCAService = new SmartCAService(configRequest);
            _dbService = new DbService();   
        }

        [WebMethod]
        [SoapHeader("AuthorizeHeader", Direction = SoapHeaderDirection.In)]
        public ERS_Response Authorize(string userName, string Md5Password)
        {
            bool isAuthed = false;
            try
            {
                if (AuthorizeHeader == null)
                {
                    Utilities.logger.ErrorLog("Cannot find AuthorizeHeader", "Authorization failed");
                    return new ERS_Response("Cannot find AuthorizeHeader", false);
                }
                if (!AuthorizeHeader.SecretKey.Equals(Utilities.glbVar.SecretKey))
                {
                    Utilities.logger.ErrorLog("SecretKey is invalid", "Authorization failed");
                    return new ERS_Response("SecretKey is invalid", false);
                }
                DataTable dtAuth = _dbService.GetDataTable("SELECT PASS FROM DOANH_NGHIEP WHERE MA_SO_THUE = @MST AND TRANG_THAI=1 AND IS_XAC_THUC=1 AND IS_KHOA=0", "",
                    new SqlParameter[]
                    {
                        new SqlParameter("MST",userName)
                    }
                    );
                if (dtAuth.Rows.Count == 0)
                {
                    Utilities.logger.ErrorLog("Username is incorrect", "Authorization failed");
                    return new ERS_Response("Username is incorrect", false);
                }
                isAuthed = dtAuth.AsEnumerable().Any(r => r["PASS"].SafeString() == Md5Password);
                if (!isAuthed)
                {
                    return new ERS_Response("Username not found", false);
                }
                return new ERS_Response("Authorized", true);
            }
            catch (Exception ex)
            {
                Utilities.logger.ErrorLog(ex, "Authorize");
                return new ERS_Response(ex.Message, false);
            }
        }

        [WebMethod]
        [SoapHeader("AuthorizeHeader", Direction = SoapHeaderDirection.In)]
        public ERS_Response GetCertificate_VNPT(string serialNumber = "")
        {
            try
            {
                if(serialNumber != "")
                {
                    UserCertificate cert = _smartCAService.GetAccountCert(VNPT_URI.uriGetCert_test, serialNumber);
                    if (cert != null) 
                    {
                        return new ERS_Response("Success", true, cert);
                    }
                    return new ERS_Response("Certificate not found", false);
                }
                List<UserCertificate> listCerts = _smartCAService.GetListAccountCert(VNPT_URI.uriGetCert_test);
                if (listCerts.Count > 0)
                {
                    return new ERS_Response("Success", true, listCerts ); 
                }
                return new ERS_Response("Certificate not found",false);
            }
            catch (Exception ex)
            {
                Utilities.logger.ErrorLog(ex, "GetCertificate_VNPT");
                return new ERS_Response($"Server error: {ex.Message}", false);
            }
        }

        [WebMethod]
        [SoapHeader("AuthorizeHeader", Direction = SoapHeaderDirection.In)]
        public ERS_Response SendFileToSign_VNPT(List<SignFile> sign_files, string serialNumber = "") 
        {
            try
            {
                ResSign res = _smartCAService.Sign(VNPT_URI.uriSign_test, sign_files, serialNumber);
                if (res.status_code != 200 && res.data == null)
                {
                    return new ERS_Response("Cannot send file to vnpt server", false);
                }
                return new ERS_Response("Send file successfully, waiting user to sign on app", true, res.data);
            }
            catch (Exception ex)
            {
                Utilities.logger.ErrorLog(ex, "SendFileToSign_VNPT");
                return new ERS_Response($"Server error: {ex.Message}", false);
            }
        }

        

        private bool SignHash_PDF(SignFileInfo unsignFile, string certBase64, ref string data_to_be_sign)
        {
            try
            {
                IHashSigner signer = HashSignerFactory.GenerateSigner(unsignFile.Data, certBase64, null, HashSignerFactory.PDF);
                signer.SetHashAlgorithm(MessageDigestAlgorithm.SHA256);

                #region Optional -----------------------------------
                // Property: Lý do ký số
                ((PdfHashSigner)signer).SetReason("Xác nhận tài liệu");
                // Kiểu hiển thị chữ ký (OPTIONAL/DEFAULT=TEXT_WITH_BACKGROUND)
                ((PdfHashSigner)signer).SetRenderingMode(PdfHashSigner.RenderMode.TEXT_ONLY);
                // Nội dung text trên chữ ký (OPTIONAL)
                ((PdfHashSigner)signer).SetLayer2Text("Ngày ký: 15/03/2022 \n Người ký: Ngô Quang Đạt \n Nơi ký: VNPT-IT");
                // Fontsize cho text trên chữ ký (OPTIONAL/DEFAULT = 10)
                ((PdfHashSigner)signer).SetFontSize(12);
                //((PdfHashSigner)signer).SetLayer2Text("yahooooooooooooooooooooooooooo");
                // Màu text trên chữ ký (OPTIONAL/DEFAULT=000000)
                ((PdfHashSigner)signer).SetFontColor("ff0000");
                // Kiểu chữ trên chữ ký
                ((PdfHashSigner)signer).SetFontStyle(PdfHashSigner.FontStyle.Normal);
                // Font chữ trên chữ ký
                ((PdfHashSigner)signer).SetFontName(PdfHashSigner.FontName.Arial);

                // Hiển thị ảnh chữ ký tại nhiều vị trí trên tài liệu
                RectanglePosition recPos = unsignFile.SignPosition;
                ((PdfHashSigner)signer).AddSignatureView(new PdfSignatureView
                {
                    Rectangle = $"{recPos.rx},{recPos.ry},{recPos.lx},{recPos.ly}",
                    Page = unsignFile.PageSign
                });

                #endregion -----------------------------------------            

                SignerProfile profile = signer.GetSignerProfile();

                string profileJson = JsonConvert.SerializeObject(profile);

                string hashValue = Convert.ToBase64String(profile.SecondHashBytes);

                data_to_be_sign = BitConverter.ToString(Convert.FromBase64String(hashValue)).Replace("-", "").ToLower();

                string tempFolder = Path.GetTempPath();
                File.AppendAllText(tempFolder + data_to_be_sign + ".txt", profileJson);
                return true;
            }
            catch (Exception ex)
            {
                Utilities.logger.ErrorLog(ex, "SignHash_PDF");
                data_to_be_sign = "";
                return false;
            }
            
        }
        
    }
}
