using ERS_Domain;
using ERS_Domain.CAService;
using ERS_Domain.clsUtilities;
using ERS_Domain.Model;
using ERS_Domain.Response;
using IntrustCA_Domain;
using IntrustCA_Domain.Dtos;
using Org.BouncyCastle.Asn1.Pkcs;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
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
        private SmartCAService _smartCAService;
        private CA2SigningService _ca2Service;

        public RemoteSigningService_v2()
        {
            _dbService = new DbService();
            _coreService = new CoreService(_dbService);
            _smartCAService = new SmartCAService(Utilities.glbVar.ConfigRequest);
            _ca2Service = new CA2SigningService();
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

        [WebMethod(Description = "Phương thức lấy chữ ký số từ server của bên thứ ba, truyền serial number để lấy chính xác chữ ký số nếu tài khoản có nhiều chữ ký số.")]
        [SoapHeader("AuthorizeHeader", Direction = SoapHeaderDirection.In)]
        public ERS_Response GetCertificate_VNPT(RemoteSigningProvider provider, string uid, string serialNumber = "")
        {
            try
            {
                //lay cks ko can xac thuc ivan
                List<UserCertificate> lstCert = new List<UserCertificate>();
                switch (provider)
                {
                    case RemoteSigningProvider.VNPT:
                        {
                            UserCertificate[] certs = _smartCAService.GetListAccountCert(VNPT_URI.uriGetCert, uid);
                            if (certs == null) return new ERS_Response("Không tìm thấy chữ ký số", false);
                            return new ERS_Response("Thành công", true, certs);
                        }
                    case RemoteSigningProvider.Intrust:
                        {
                            GetCertificateRequest req = new GetCertificateRequest
                            {
                                user_id = uid,
                                serial_number = serialNumber,
                            };
                            var res = IntrustSigningCoreService.GetCertificate(req);
                            if (res == null || res.status_code != 0 || res.certificates == null || res.certificates.Length == 0)
                            {
                                throw new Exception($"Get certificate error: {res?.error_desc ?? "No response from IntrustCA"}");
                            }
                            foreach (ICACertificate IntrustCert in res.certificates)
                            {
                                UserCertificate cert = new UserCertificate
                                {
                                    serial_number = IntrustCert.cert_serial,
                                    cert_valid_to = IntrustCert.cert_valid_to.SafeDateTime(),
                                    cert_valid_from = IntrustCert.cert_valid_from.SafeDateTime(),
                                    cert_subject = IntrustCert.cert_provider,
                                    cert_status = IntrustCert.cert_valid_to.SafeDateTime() < DateTime.Now ? "Hết hạn" : "Đang hoạt động",
                                };
                                lstCert.Add(cert);
                            }
                            lstCert.ToArray();
                            return new ERS_Response("Thành công", true, lstCert.ToArray());
                        }
                    case RemoteSigningProvider.CA2:
                        {
                            //ko dc block thread cua request soapservice
                            CA2Response<CA2Certificates> res =Task.Run(() =>
                                _ca2Service.GetCertificates(uid, Guid.NewGuid().ToString(), serialNumber)
                            ).Result;
                            if (res == null || res?.status_code != 200)
                            {
                                return new ERS_Response($"Get certificate error: {res?.message ?? "No response from IntrustCA"}");
                            }
                            foreach (CA2Certificate ca2Cert in res.data.user_certificates)
                            {
                                X509Certificate2 certX509 = new X509Certificate2(Convert.FromBase64String(ca2Cert.cert_data));
                                UserCertificate cert = new UserCertificate
                                {
                                    serial_number = ca2Cert.serial_number,
                                    cert_valid_to = certX509.NotAfter.SafeDateTime(),
                                    cert_valid_from = certX509.NotBefore.SafeDateTime(),
                                    cert_subject = certX509.Subject,
                                    cert_status = certX509.NotAfter.SafeDateTime() < DateTime.Now ? "Hết hạn" : "Đang hoạt động",
                                };
                                lstCert.Add(cert);
                            }
                            return new ERS_Response("Thành công", true, lstCert.ToArray());
                        }
                    default:
                        {
                            return new ERS_Response("Nhà cung cấp dịch vụ không hợp lệ", false);
                        }
                }
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