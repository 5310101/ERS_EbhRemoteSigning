using EBH_RemoteSigning_Service_ERS.clsUtilities;
using EBH_RemoteSigning_Service_ERS.SmartCAService;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.Services;
using System.Web.Services.Protocols;

namespace EBH_RemoteSigning_Service_ERS
{
    public class Authorize : SoapHeader
    {
        public int SecretKey { get; set; }
    }

    /// <summary>
    /// Summary description for RemoteSigningService
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    // To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
    // [System.Web.Script.Services.ScriptService]
    public class RemoteSigningService : System.Web.Services.WebService
    {
        public Authorize AuthorizeHeader;
        private ISmartCAService _smartCAService;

        public RemoteSigningService()
        {
            _smartCAService = new SmartCAService();
        }

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
                DataTable dtAuth = Utilities.dbService.GetDataTable("SELECT PASS FROM DOANH_NGHIEP WHERE MA_SO_THUE = @MST AND TRANG_THAI=1 AND IS_XAC_THUC=1 AND IS_KHOA=0", "",
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
        public ERS_Response SendFileToSign(string user, string password)
        {
            try
            {
                ERS_Response auth = Authorize(user, password);
                if (!auth.success)
                {
                    return auth;
                }

            }
            catch (Exception)
            {

                throw;
            }
        }

        
    }
}
