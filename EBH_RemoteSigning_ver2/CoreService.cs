using ERS_Domain;
using ERS_Domain.CAService;
using ERS_Domain.clsUtilities;
using ERS_Domain.Model;
using ERS_Domain.Response;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;

namespace EBH_RemoteSigning_ver2
{
    public class CoreService
    {
        //private IRemoteSignService _signService;
        private DbService _dbService;

        public CoreService( DbService dbService)
        {
            //_signService = signService;
            _dbService = dbService;
        }

        public bool SaveToKhai(List<ToKhaiInfo> tokhais, string GuidHS, string uid, string serialNumber = "")
        {
            //Tao thu muc chua ho so ky
            string pathTempHS = Path.Combine(Utilities.globalPath.SignedTempFolder, $"{GuidHS}");
            if (!Directory.Exists(pathTempHS))
            {
                Directory.CreateDirectory(pathTempHS);
            }
            try
            {
                //Tao cac file de ky
                foreach (ToKhaiInfo tokhai in tokhais)
                {
                    string pathFile = Path.Combine(pathTempHS, tokhai.TenFile);
                    File.WriteAllBytes(pathFile, tokhai.Data);
                }

                //luu cac to khai vao database
                bool isInserted = InsertDatabase_ToKhai(tokhais, GuidHS, uid, serialNumber);
                //neu insert loi delete thu muc temp cua ho so
                if (!isInserted)
                {
                    Directory.Delete(pathTempHS, true);
                }
                return isInserted;
            }
            catch (Exception ex)
            {
                //Xoa thu muc chua temp file cua hoso neu loi
                if (pathTempHS != "" || Directory.Exists(pathTempHS))
                {
                    Directory.Delete(pathTempHS, true);
                }
                Utilities.logger.ErrorLog(ex, "SaveToKhai");
                return false;
            }
        }

        public bool SaveHSDKFile(HoSoDKInfo hsDK, string base64Data)
        {
            //Tao thu muc chua ho so ky
            string pathTempHS = Path.Combine(Utilities.globalPath.SignedTempFolder, $"{hsDK.GuidHS}");
            string filePath = Path.Combine(pathTempHS, $"{hsDK.MaNghiepVu}.xml");
            try
            {
                if (!Directory.Exists(pathTempHS))
                {
                    Directory.CreateDirectory(pathTempHS);
                }
                string data = base64Data.FromBase64ToString();
                File.WriteAllText(filePath, data);
                return true;
            }
            catch (Exception ex)
            {
                //Xoa thu muc chua temp file cua hoso neu loi
                if (pathTempHS != "" || Directory.Exists(pathTempHS))
                {
                    Directory.Delete(pathTempHS, true);
                }
                Utilities.logger.ErrorLog(ex, "SaveHSDKFile");
                return false;
            }
        }

        private bool InsertDatabase_ToKhai(List<ToKhaiInfo> tokhais, string GuidHS, string uid, string serialNumber = "")
        {
            string pathTempHS = Path.Combine(Utilities.globalPath.SignedTempFolder, $"{GuidHS}");
            using (SqlConnection conn = new SqlConnection(_dbService.ConnStr))
            {
                conn.Open();
                SqlTransaction trans = conn.BeginTransaction();
                try
                {
                    foreach (ToKhaiInfo tokhai in tokhais)
                    {
                        string TSQL = "INSERT INTO ToKhai_RS (GuidHS,TenToKhai,LoaiFile,MoTa,NgayGui,TrangThai,FilePath,LastGet,uid,SerialNumber) VALUES (@GuidHS,@TenToKhai,@LoaiFile,@Mota,@NgayGui,@TrangThai,@FilePath,@LastGet,@uid,@SerialNumber)";
                        var listParams = new SqlParameter[]
                        {
                            new SqlParameter("@GuidHS",GuidHS),
                            new SqlParameter("@TenToKhai",tokhai.TenFile),
                            new SqlParameter("@LoaiFile", (int)tokhai.Type),
                            new SqlParameter("@MoTa",tokhai.TenToKhai),
                            new SqlParameter("@NgayGui",DateTime.Now),
                            new SqlParameter("@TrangThai", (int)TrangThaiFile.TaoMoi),
                            new SqlParameter("@FilePath", Path.Combine(pathTempHS,tokhai.TenFile)),
                            new SqlParameter("@LastGet", DateTime.Now),
                            new SqlParameter("@uid", uid),
                            new SqlParameter("@SerialNumber",serialNumber),
                        };
                        using (SqlCommand command = new SqlCommand(TSQL, conn, trans))
                        {
                            command.Parameters.AddRange(listParams);
                            command.CommandType = System.Data.CommandType.Text;
                            command.CommandTimeout = 60;
                            var result = command.ExecuteNonQuery();
                            if (result <= 0)
                            {
                                throw new Exception("Insert Failed");
                            }
                        }
                    }
                    trans.Commit();
                    return true;
                }
                catch (Exception ex)
                {
                    trans.Rollback();
                    Utilities.logger.ErrorLog(ex, "InsertDatabase_ToKhai", GuidHS);
                    return false;
                }
            }
        }

        //neu nguoi dung co tu 2 cks tro len se dung ham nay de lay
        //public UserCertificate[] GetListUserCertificateVNPT(string uid)
        //{
        //    UserCertificate[] certs = _signService.GetListAccountCert(VNPT_URI.uriGetCert, uid);
        //    return certs;
        //}

        //Cac ham lien quan den tao lap hoso
        public bool InsertHoSoNew(HoSoInfo hoso, string uid, string serialNumber ,int CAProvider)
        {
            try
            {
                string TSQL = "INSERT INTO HoSo_RS (Guid,TenHS,MaNV,NgayGui,TenDonVi,FromMST,FromMDV,LoaiDoiTuong,MaCQBH,NguoiKy,DienThoai,TrangThai,LastGet,uid,SerialNumber,typeDK,CAProvider) VALUES (@Guid,@TenHS,@MaNV,@NgayGui,@TenDonVi,@FromMST,@FromMDV,@LoaiDoiTuong,@MaCQBH,@NguoiKy,@DienThoai,@TrangThai,@LastGet,@uid,@SerialNumber,@typeDK,@CAProvider)";
                SqlParameter[] listParams = new SqlParameter[] {
                        new SqlParameter("@Guid",hoso.GuidHS),
                        new SqlParameter("@TenHS",hoso.TenThuTuc),
                        new SqlParameter("@MaNV",hoso.MaHoSo),
                        new SqlParameter("@NgayGui",DateTime.Now),
                        new SqlParameter("@TenDonVi",hoso.DonVi.TenDonVi),
                        new SqlParameter("@FromMST",hoso.DonVi.MaSoThue),
                        new SqlParameter("@FromMDV",hoso.DonVi.MaDonVi),
                        new SqlParameter("@LoaiDoiTuong",hoso.DonVi.LoaiDoiTuong),
                        new SqlParameter("@MaCQBH",hoso.DonVi.CoQuanBHXH),
                        new SqlParameter("@NguoiKy",hoso.DonVi.NguoiKy),
                        new SqlParameter("@DienThoai",hoso.DonVi.DienThoai),
                        new SqlParameter("@TrangThai",(int)TrangThaiHoso.ChuaTaoFile),
                        new SqlParameter("@LastGet",DateTime.Now),
                        new SqlParameter("@uid",uid),
                        new SqlParameter("@SerialNumber",serialNumber),
                        new SqlParameter("@typeDK",System.Data.SqlDbType.Int){ Value = 0},
                        new SqlParameter("@CAProvider",System.Data.SqlDbType.Int){ Value = CAProvider},
                };
                bool isSuccess = _dbService.ExecQuery_Tran(TSQL, "", listParams);
                return isSuccess;
            }
            catch (Exception ex)
            {
                Utilities.logger.ErrorLog(ex, "InsertHoSoNew_VNPT");
                return false;
            }
        }

        //Insert HSDK
        public bool InsertHoSoDKNew(HoSoDKInfo hoso, string uid, string serialNumber, int typeDK, int CAProvider)
        {
            try
            {
                string TSQL = "INSERT INTO HoSo_RS (Guid,TenHS,MaNV,NgayGui,TenDonVi,FromMST,FromMDV,LoaiDoiTuong,MaCQBH,NguoiKy,DienThoai,TrangThai,LastGet,uid,SerialNumber,typeDK,CAProvider) VALUES (@Guid,@TenHS,@MaNV,@NgayGui,@TenDonVi,@FromMST,@FromMDV,@LoaiDoiTuong,@MaCQBH,@NguoiKy,@DienThoai,@TrangThai,@LastGet,@uid,@SerialNumber,@typeDK,@CAProvider)";
                SqlParameter[] listParams = new SqlParameter[] {
                        new SqlParameter("@Guid",hoso.GuidHS),
                        new SqlParameter("@TenHS",hoso.TenHoSo),
                        new SqlParameter("@MaNV",hoso.MaNghiepVu),
                        new SqlParameter("@NgayGui",DateTime.Now),
                        new SqlParameter("@TenDonVi",hoso.HoSoDK.TenDoiTuong),
                        new SqlParameter("@FromMST",hoso.HoSoDK.MaSoThue),
                        new SqlParameter("@FromMDV",""),
                        new SqlParameter("@LoaiDoiTuong",hoso.HoSoDK.LoaiDoiTuong),
                        new SqlParameter("@MaCQBH",hoso.HoSoDK.MaCoQuan),
                        new SqlParameter("@NguoiKy",hoso.HoSoDK.NguoiLienHe),
                        new SqlParameter("@DienThoai",hoso.HoSoDK.DienThoai),
                        new SqlParameter("@TrangThai",(int)TrangThaiHoso.ChuaTaoFile),
                        new SqlParameter("@LastGet",DateTime.Now),
                        new SqlParameter("@uid",uid),
                        new SqlParameter("@serialNumber",serialNumber),
                        new SqlParameter("@typeDK", System.Data.SqlDbType.Int){ Value = typeDK},
                        new SqlParameter("@CAProvider", System.Data.SqlDbType.Int){ Value = CAProvider}
                };
                bool isSuccess = _dbService.ExecQuery_Tran(TSQL, "", listParams);
                return isSuccess;
            }
            catch (Exception ex)
            {
                Utilities.logger.ErrorLog(ex, "InsertHoSoNew_VNPT");
                return false;
            }
        }

        public bool InsertHSDKLanDau(HoSoDKInfo hsDK)
        {
            try
            {
                string TSQL1 = "SELECT * FROM HSDKLanDau WHERE GuidHS=@Guid";
                var dt = _dbService.GetDataTable(TSQL1, "", new SqlParameter[]
                {
                    new SqlParameter("@Guid", hsDK.GuidHS)
                });
                if (dt.Rows.Count > 0)
                {
                    _dbService.ExecQuery("DELETE FROM HSDKlanDau WHERE GuidHS=@Guid", "", new SqlParameter[]
                    {
                        new SqlParameter("@Guid", hsDK.GuidHS)
                    });
                }

                string TSQL = "INSERT INTO HSDKLanDau (GuidHS,TenCoQuan,MaCoQuan,LoaiDoiTuong,TenDoiTuong,MaSoThue,DienThoai,Email,NguoiLienHe,DiaChi,DiaChiLienHe,DienThoaiLienHe,NgayLap,NgayDangKy,PTNhanKetQua) VALUES (@GuidHS,@TenCoQuan,@MaCoQuan,@LoaiDoiTuong,@TenDoiTuong,@MaSoThue,@DienThoai,@Email,@NguoiLienHe,@DiaChi,@DiaChiLienHe,@DienThoaiLienHe,@NgayLap,@NgayDangKy,@PTNhanKetQua)";
                SqlParameter[] listParams = new SqlParameter[] {
                        new SqlParameter("@GuidHS",hsDK.GuidHS),
                        new SqlParameter("@TenCoQuan",hsDK.HoSoDK.TenCoQuan),
                        new SqlParameter("@MaCoQuan",hsDK.HoSoDK.MaCoQuan),
                        new SqlParameter("@LoaiDoiTuong",hsDK.HoSoDK.LoaiDoiTuong),
                        new SqlParameter("@TenDoiTuong",hsDK.HoSoDK.TenDoiTuong),
                        new SqlParameter("@MaSoThue",hsDK.HoSoDK.MaSoThue),
                        new SqlParameter("@DienThoai",hsDK.HoSoDK.DienThoai),
                        new SqlParameter("@Email",hsDK.HoSoDK.Email),
                        new SqlParameter("@NguoiLienHe",hsDK.HoSoDK.NguoiLienHe),
                        new SqlParameter("@DiaChi",hsDK.HoSoDK.DiaChi),
                        new SqlParameter("@DiaChiLienHe",hsDK.HoSoDK.DiaChiLienHe),
                        new SqlParameter("@DienThoaiLienHe",hsDK.HoSoDK.DienThoaiLienHe),
                        new SqlParameter("@NgayLap",hsDK.HoSoDK.NgayLap),
                        new SqlParameter("@NgayDangKy",hsDK.HoSoDK.NgayDangKy),
                        new SqlParameter("@PTNhanKetQua",hsDK.HoSoDK.PTNhanKetQua),
                };
                bool isSuccess = _dbService.ExecQuery_Tran(TSQL, "", listParams);
                return isSuccess;
            }
            catch (Exception ex)
            {
                Utilities.logger.ErrorLog(ex, "InsertHSDKLanDau");
                return false;
            }
        }
    }
}