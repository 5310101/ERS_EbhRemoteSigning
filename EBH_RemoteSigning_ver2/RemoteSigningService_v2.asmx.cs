using ERS_Domain;
using ERS_Domain.CAService;
using ERS_Domain.clsUtilities;
using ERS_Domain.Model;
using ERS_Domain.Response;
using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Web.Services;
using System.Web.Services.Protocols;

namespace EBH_RemoteSigning_ver2
{
    public class Authorize : SoapHeader
    {
        public string SecretKey { get; set; }
    }

    /// <summary>
    /// Summary description for RemoteSigningService_v2
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    public class RemoteSigningService_v2 : System.Web.Services.WebService
    {
        public Authorize AuthorizeHeader;
        private CoreService _coreService;
        private DbService _dbService;

        public RemoteSigningService_v2()
        {
            _dbService = new DbService();
            _coreService = new CoreService(_dbService);
        }

        [WebMethod(Description = "Phương thức xác thực cho SOAP service.")]
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
                    return new ERS_Response("Password is incorrect", false);
                }
                return new ERS_Response("Authorized", true);
            }
            catch (Exception ex)
            {
                Utilities.logger.ErrorLog(ex, "Authorize");
                return new ERS_Response(ex.Message, false);
            }
        }

        [WebMethod(Description = "Phương thức lấy chữ ký số từ server VNPT, truyền serial number để lấy chính xác chữ ký số nếu tài khoản có nhiều chữ ký số.")]
        [SoapHeader("AuthorizeHeader", Direction = SoapHeaderDirection.In)]
        public ERS_Response GetCertificate_VNPT(string uid, string serialNumber = "")
        {
            try
            {
                //Lay cks ko can xac thuc ivan
                //ERS_Response auth = UserAuthorize(userName, password);
                //if (!auth.success)
                //{
                //    return auth;
                //}

                SmartCAService smartCAService = new SmartCAService(Utilities.glbVar.ConfigRequest);
                //_coreService = new CoreService(smartCAService, _dbService);
                UserCertificate[] certs = smartCAService.GetListAccountCert(VNPT_URI.uriGetCert, uid);
                if (certs == null) return new ERS_Response("Không tìm thấy chữ ký số", false);
                return new ERS_Response("Thành công", true, certs);
            }
            catch (Exception ex)
            {
                Utilities.logger.ErrorLog(ex, "GetCertificate_VNPT");
                return new ERS_Response($"Server error: {ex.Message}", false);
            }
        }

        [WebMethod(Description = "Phương thức gửi file lên server để thực hiện ký.")]
        [SoapHeader("AuthorizeHeader", Direction = SoapHeaderDirection.In)]
        public ERS_Response SendFileSign(RemoteSigningProvider signProvider, string uid, string username, string password, HoSoInfo hoso, string serialNumber = "")
        {
            try
            {
                //xac thuc
                ERS_Response result = UserAuthorize(username, password);
                if (!result.success)
                {
                    return result;
                }
                //tien hanh ky cac to khai neu ky dc ko loi thi luu hoso vao db
                //chon nha cung cap dich vu
                //SmartCAService smartCAService = new SmartCAService(Utilities.glbVar.ConfigRequest);
                //_coreService = new CoreService( _dbService);
                string TSQL = "SELECT * FROM HoSo_RS WHERE Guid=@Guid";
                DataTable dt = _dbService.GetDataTable(TSQL, "", new SqlParameter[] { new SqlParameter("@Guid", hoso.GuidHS) });
                if (dt.Rows.Count > 0)
                {
                    // neu da ton tai thi xoa ban ghi cu truoc khi insert ban ghi moi
                    _dbService.ExecQuery("DELETE FROM ToKhai_RS WHERE GuidHS=@Guid", "", new SqlParameter[] { new SqlParameter("@Guid", hoso.GuidHS) });
                    _dbService.ExecQuery("DELETE FROM HoSo_RS WHERE Guid=@Guid", "", new SqlParameter[] { new SqlParameter("@Guid", hoso.GuidHS) });
                }
                bool isSaveFile = _coreService.SaveToKhai(hoso.ToKhais, hoso.GuidHS, uid, serialNumber);
                if (!isSaveFile)
                {
                    return new ERS_Response("Không gửi file thành công", false);
                }
                //Tao moi hoso va insert vao database
                //Check xem hoso da ton tai chua, trong th ky lai

                bool isSuccess = _coreService.InsertHoSoNew(hoso, uid, serialNumber, (int)signProvider);
                if (!isSuccess)
                {
                    Utilities.logger.ErrorLog($"Hồ sơ lưu vào lỗi vào database: {hoso.GuidHS}", "Hồ sơ lưu lỗi");
                    return new ERS_Response("Có lỗi khi lưu dữ liệu hồ sơ trên server", false);
                }
                return new ERS_Response("Chờ xác thực trên app ký của nhà cung cấp dịch vụ CA", true);
            }
            catch (Exception ex)
            {
                Utilities.logger.ErrorLog(ex, "SendFileSign", hoso.GuidHS);
                return new ERS_Response($"Lỗi Server: {ex.Message}", false);
            }
        }

        [WebMethod(Description = "Phương thức gửi file lên server để thực hiện ký (hồ sơ đăng ký).")]
        [SoapHeader("AuthorizeHeader", Direction = SoapHeaderDirection.In)]
        public ERS_Response SendFileSignDK(RemoteSigningProvider signProvider, string uid, HoSoDKInfo hsDK, string base64DataDK = "", string serialNumber = "")
        {
            try
            {
                //Ho so dang ky se ko xac thuc
                //ERS_Response result = UserAuthorize(username, password);
                //if (!result.success)
                //{
                //    return result;
                //}
                //tien hanh ky cac to khai neu ky dc ko loi thi luu hoso vao db
                //chon nha cung cap dich vu
                    //SmartCAService smartCAService = new SmartCAService(Utilities.glbVar.ConfigRequest);
                    //_coreService = new CoreService( _dbService);
                    string TSQL = "SELECT * FROM HoSo_RS WHERE Guid=@Guid";
                    DataTable dt = _dbService.GetDataTable(TSQL, "", new SqlParameter[] { new SqlParameter("@Guid", hsDK.GuidHS) });
                    if (dt.Rows.Count > 0)
                    {
                        // neu da ton tai thi xoa ban ghi cu truoc khi insert ban ghi moi
                        if (hsDK.ToKhais != null)
                        {
                            _dbService.ExecQuery("DELETE FROM ToKhai_RS WHERE GuidHS=@Guid", "", new SqlParameter[] { new SqlParameter("@Guid", hsDK.GuidHS) });
                        }

                        _dbService.ExecQuery("DELETE FROM HoSo_RS WHERE Guid=@Guid", "", new SqlParameter[] { new SqlParameter("@Guid", hsDK.GuidHS) });
                    }
                    bool isSaveFile = true;
                    //Loai dang ky 1 = 04,05,06, 2 la dk ma lan dau
                    int typeDK = 1;
                    // chi dk ma lan dau moi co to khai va file dinh kem
                    if (hsDK.ToKhais is null || hsDK.ToKhais.Count > 0)
                    {
                        isSaveFile = _coreService.SaveToKhai(hsDK.ToKhais, hsDK.GuidHS, uid, serialNumber);
                        bool isInsertHSDKLanDau = _coreService.InsertHSDKLanDau(hsDK);
                        typeDK = 2;
                    }
                    else
                    {
                        //neu ko ton tai tokhais tuc la ky 04,05,06 gửi trực tiếp base64 của file .xml lên
                        isSaveFile = _coreService.SaveHSDKFile(hsDK, base64DataDK);
                    }

                    if (!isSaveFile)
                    {
                        return new ERS_Response("Không gửi file thành công", false);
                    }
                    //Tao moi hoso va insert vao database
                    //Check xem hoso da ton tai chua, trong th ky lai

                    bool isSuccess = _coreService.InsertHoSoDKNew(hsDK, uid, serialNumber, typeDK, (int)signProvider);
                    if (!isSuccess)
                    {
                        Utilities.logger.ErrorLog($"Hồ sơ lưu vào lỗi vào database: {hsDK.GuidHS}", "Hồ sơ lưu lỗi");
                        return new ERS_Response("Có lỗi khi lưu dữ liệu hồ sơ trên server", false);
                    }
                    return new ERS_Response("Chờ xác thực trên app ký của nhà cung cấp dịch vụ CA", true);
              
            }
            catch (Exception ex)
            {
                Utilities.logger.ErrorLog(ex, "SendFileSign", hsDK.GuidHS);
                return new ERS_Response($"Lỗi Server: {ex.Message}", false);
            }
        }

        [WebMethod(Description = "Phương thức lấy kết quả ký số từ server.")]
        [SoapHeader("AuthorizeHeader", Direction = SoapHeaderDirection.In)]
        public ERS_Response GetFileSigned(string username, string password, string HoSoGuid)
        {
            try
            {
                string TSQL = "SELECT * FROM HoSo_RS WHERE Guid=@Guid";
                DataTable dt = _dbService.GetDataTable(TSQL, "", new SqlParameter[]
                {
                    new SqlParameter("@Guid", HoSoGuid)
                });
                if (dt.Rows.Count == 0)
                {
                    return new ERS_Response($"Không tồn tại file hồ sơ: {HoSoGuid}", true);
                }
                int trangThai = MethodLibrary.SafeNumber<int>(dt.Rows[0]["TrangThai"]);
                int typeDK = MethodLibrary.SafeNumber<int>(dt.Rows[0]["typeDK"]);
                //xac thuc
                if (typeDK == 0)
                {
                    ERS_Response result = UserAuthorize(username, password);
                    if (!result.success)
                    {
                        return result;
                    }
                }

                if ((TrangThaiHoso)trangThai == TrangThaiHoso.KyLoi)
                {
                    return new ERS_Response($"SIGNERROR:{MethodLibrary.SafeString(dt.Rows[0]["ErrMsg"])}", true);
                }
                if ((TrangThaiHoso)trangThai == TrangThaiHoso.HetHan)
                {
                    return new ERS_Response("EXPIRED", true);
                }
                if ((TrangThaiHoso)trangThai == TrangThaiHoso.DaLayKetQua)
                {
                    return new ERS_Response("RESULT_RETURNED_BEFORE", true);
                }
                if ((TrangThaiHoso)trangThai == TrangThaiHoso.DaKy)
                {
                    string filePath = MethodLibrary.SafeString(dt.Rows[0]["FilePath"]);
                    byte[] data = File.ReadAllBytes(filePath);
                    string base64Data = Convert.ToBase64String(data);

                    _dbService.ExecQuery("UPDATE HoSo_RS SET TrangThai=5 WHERE Guid=@Guid", "", new SqlParameter[]
                    {
                        new SqlParameter("@Guid",HoSoGuid)
                    });
                    string FolderHSPath = Path.GetDirectoryName(filePath);
                    Directory.Delete(FolderHSPath, true);
                    return new ERS_Response("SIGN_SUCCESS", true, base64Data);
                }
                return new ERS_Response("PENDING", true);
            }
            catch (Exception ex)
            {
                Utilities.logger.ErrorLog(ex, "GetFileSigned", HoSoGuid);
                return new ERS_Response(ex.Message, false);
            }
        }
    }
}