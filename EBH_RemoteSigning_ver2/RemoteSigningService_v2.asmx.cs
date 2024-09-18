using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.Services;
using System.Web.Services.Protocols;
using ERS_Domain.CAService;
using ERS_Domain.clsUtilities;
using ERS_Domain;
using ERS_Domain.Model;
using System.Threading.Tasks;

namespace EBH_RemoteSigning_ver2
{
    /// <summary>
    /// Summary description for RemoteSigningService_v2
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    // To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
    // [System.Web.Script.Services.ScriptService]
    public class RemoteSigningService_v2 : System.Web.Services.WebService
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
            private CoreService _coreService;
            private readonly DbService _dbService;

            public RemoteSigningService()
            {
                _dbService = new DbService();
            }

            [WebMethod]
            [SoapHeader("AuthorizeHeader", Direction = SoapHeaderDirection.In)]
            public async Task<ERS_Response> UserAuthorize(string userName, string Md5Password)
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
                    DataTable dtAuth = await _dbService.GetDataTableAsync("SELECT PASS FROM DOANH_NGHIEP WHERE MA_SO_THUE = @MST AND TRANG_THAI=1 AND IS_XAC_THUC=1 AND IS_KHOA=0", "",
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
            public async Task<ERS_Response> SendFileSign(RemoteSigningProvider signProvider, string uid, string username ,string password, HoSoInfo hoso)
            {
                try
                {
                    //xac thuc
                    ERS_Response result = await UserAuthorize(username, password);
                    if (!result.success)
                    {
                        return result;
                    }
                    //tien hanh ky cac to khai neu ky dc ko loi thi luu hoso vao db
                    //chon nha cung cap dich vu
                    if(signProvider == RemoteSigningProvider.VNPT)
                    {
                        SmartCAService smartCAService = new SmartCAService(Utilities.glbVar.ConfigRequest, uid);
                        _coreService = new CoreService(smartCAService);
                    }
                    else
                    {
                        return new ERS_Response("Hiện phần mềm mới hỗ trợ ký từ xa từ dịch vụ của VNPT", false);
                    }

                }
                catch (Exception ex)
                {
                    Utilities.logger.ErrorLog(ex, "SendFileSign", hoso.GuidHS);
                    throw;
                }
            }
        }
    }
}


