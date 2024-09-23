using EBH_RemoteSigning_Service_ERS.CAService;
using EBH_RemoteSigning_Service_ERS.clsUtilities;
using EBH_RemoteSigning_Service_ERS.Request;
using EBH_RemoteSigning_Service_ERS.Response;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web.Services;
using System.Web.Services.Protocols;
using System.Xml.Serialization;

namespace EBH_RemoteSigning_Service_ERS
{
    public class Authorize : SoapHeader
    {
        public string SecretKey { get; set; }
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
        public ERS_Response UserAuthorize(string userName, string Md5Password)
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
                DataTable dtAuth = _dbService.GetDataTable("SELECT PASS FROM DOANH_NGHIEP WITH (NOLOCK) WHERE MA_SO_THUE = @MST AND TRANG_THAI=1 AND IS_XAC_THUC=1 AND IS_KHOA=0", "",
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
        public ERS_Response GetCertificate_VNPT(string userName, string password, string uid ,string serialNumber = "")
        {
            try
            {
                ERS_Response auth = UserAuthorize(userName, password);
                if (!auth.success)
                {
                    return auth;
                }
                if (serialNumber != "")
                {
                    UserCertificate cert = _smartCAService.GetAccountCert(VNPT_URI.uriGetCert_test, uid, serialNumber);
                    if (cert != null) 
                    {
                        UserCertificate[] certs = new UserCertificate[] { cert };
                        return new ERS_Response("Success", true, certs);
                    }
                    return new ERS_Response("Certificate not found", false);
                }
                UserCertificate[] listCerts = _smartCAService.GetListAccountCert(VNPT_URI.uriGetCert_test, uid);
                if (listCerts.Length > 0)
                {
                    return new ERS_Response("Success", true, listCerts); 
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
        public ERS_Response SendFileToSign_VNPT(string userName, string password, string uid ,List<SignFile> sign_files, string serialNumber = "")
        {
            try
            {
                ERS_Response auth = UserAuthorize(userName, password);
                if (!auth.success)
                {
                    return auth;
                }
                ResSign res = _smartCAService.Sign(VNPT_URI.uriSign_test, sign_files, uid, serialNumber);
                if (res.status_code != 200 || res.data == null)
                {
                    Utilities.logger.ErrorLog("Cannot send file to vnpt server","Server vnpt error",$"Message from VNPT: {res.message}");
                    return new ERS_Response($"Cannot send file to vnpt server", false);
                }
                return new ERS_Response("Send file successfully, waiting user to sign on app", true, res.data);
            }
            catch (Exception ex)
            {
                Utilities.logger.ErrorLog(ex, "SendFileToSign_VNPT");
                return new ERS_Response($"Server error: {ex.Message}", false);
            }
        }

        private string UriGetStatus(string transId)
        {
            return $"{VNPT_URI.uriSign_test}/{transId}/status";
        }

        [WebMethod]
        [SoapHeader("AuthorizeHeader", Direction = SoapHeaderDirection.In)]
        public ERS_Response GetFileStatus_VNPT(string userName, string password,string transId)
        {
            try
            {
                ERS_Response auth = UserAuthorize(userName, password);
                if (!auth.success)
                {
                    return auth;
                }
                ResStatus res = _smartCAService.GetStatus(UriGetStatus(transId));
                if(res.status_code != 200)
                {
                    Utilities.logger.InfoLog("",transId);
                    return new ERS_Response("Cannot get response result from VNPT server", false);
                }
                if(res.message == "PENDING")
                {
                    return new ERS_Response("File is pending to confirm on SmartCA app", true, res.data );
                }
                if (res.message == "SUCCESS")
                {
                    return new ERS_Response("File is confirmed", true,  res.data );
                }
                return new ERS_Response(res.message, false );
            }
            catch (Exception ex)
            {
                Utilities.logger.ErrorLog(ex, "GetFileStatus_VNPT");
                return new ERS_Response($"Server error: {ex.Message}", false);
            }
        }
    }
}
