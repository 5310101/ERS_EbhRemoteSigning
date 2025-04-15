using ERS_Domain;
using ERS_Domain.CAService;
using ERS_Domain.clsUtilities;
using ERS_Domain.Model;
using ERS_Domain.Response;
using System;
using System.Collections.Generic;
using System.IO;
using System.Data.SqlClient;

namespace EBH_RemoteSigning_ver2
{
    public class CoreService
    {
        private IRemoteSignService _signService;
        private DbService _dbService;

        public CoreService(IRemoteSignService signService, DbService dbService)
        {
            _signService = signService;
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
                foreach(ToKhaiInfo tokhai in tokhais)
                {
                    string pathFile = Path.Combine(pathTempHS,tokhai.TenFile);
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
                Utilities.logger.ErrorLog(ex, "SignToKhai_VNPT");
                return false;
            }
        }

        private bool InsertDatabase_ToKhai(List<ToKhaiInfo> tokhais, string GuidHS, string uid, string serialNumber = "")
        {
            string pathTempHS = Path.Combine(Utilities.globalPath.SignedTempFolder, $"{GuidHS}");
            using (SqlConnection conn = new SqlConnection(_dbService.ConnStr))
            {
                conn.Open();
                SqlTransaction trans  = conn.BeginTransaction();
                try
                {
                    foreach (ToKhaiInfo tokhai in tokhais)
                    {
                        string TSQL = "INSERT INTO ToKhai_VNPT (GuidHS,TenToKhai,LoaiFile,MoTa,NgayGui,TrangThai,FilePath,LastGet,uid,SerialNumber) VALUES (@GuidHS,@TenToKhai,@LoaiFile,@Mota,@NgayGui,@TrangThai,@FilePath,@LastGet,@uid,@SerialNumber)";
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
                            if(result <= 0)
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
                    Utilities.logger.ErrorLog(ex, "InsertDatabase_ToKhai",GuidHS);
                    return false;
                }
            }

        }

        //neu nguoi dung co tu 2 cks tro len se dung ham nay de lay 
        public UserCertificate[] GetListUserCertificateVNPT(string uid)
        {
            UserCertificate[] certs = _signService.GetListAccountCert(VNPT_URI.uriGetCert,uid);
            return certs;
        }
        
        //Cac ham lien quan den tao lap hoso
        public bool InsertHoSoNew_VNPT(HoSoInfo hoso, string uid, string serialNumber)
        {
            try
            {
                string TSQL = "INSERT INTO HoSo_VNPT (Guid,TenHS,MaNV,NgayGui,TenDonVi,FromMST,FromMDV,LoaiDoiTuong,MaCQBH,NguoiKy,DienThoai,TrangThai,LastGet,uid,SerialNumber) VALUES (@Guid,@TenHS,@MaNV,@NgayGui,@TenDonVi,@FromMST,@FromMDV,@LoaiDoiTuong,@MaCQBH,@NguoiKy,@DienThoai,@TrangThai,@LastGet,@uid,@SerialNumber)";
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
                        new SqlParameter("@serialNumber",serialNumber),
                };
                bool isSuccess = _dbService.ExecQuery_Tran(TSQL,"", listParams);
                return isSuccess;   
                
            }
            catch (Exception ex)
            {
                Utilities.logger.ErrorLog(ex, "InsertHoSoNew_VNPT");
                return false;
            }
        }
    }
}