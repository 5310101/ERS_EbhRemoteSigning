using ERS_Domain.Dtos;
using ERS_Domain.Model;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERS_Domain.clsUtilities
{
    public static class HosoBHXHHelper
    {
        public static string GetNodeSignXml(this string fileName)
        {
            switch (fileName)
            {
                case "TK1-TS-595":
                    return "TK1-TS/Cky";
                case "D02-TS-595":
                    return "D02-TS/Cky";
                case "D03-TS-595":
                    return "D03-TS/Cky";
                case "D05-TS-595":
                    return "D05-TS/Cky";
                case "M01B-HSB":
                    return "M01B-HSB/Cky";
                case "05A-HSB":
                    return "M05A-HSB/Cky";
                case "D01-TS-595":
                    return "D01-TS/Cky";
                case "TK3-TS":
                    return "TK3-TS/Cky";
                case "BHXHDienTu":
                case "04_DK-IVAN":
                case "05_SD-IVAN":
                case "06_NG-IVAN":
                    return "Hoso/CKy_Dvi";
                default: throw new Exception($"Unknown file name {fileName}");
            }
        }

        public static void CreateFileBHXHDienTu(this HoSoMessage hs, string pathBHXHDienTu)
        {
            List<FileToKhai> listTK = new List<FileToKhai>();
            foreach (ToKhai signedTK in hs.toKhais)
            {
                byte[] tkDaKy = File.ReadAllBytes(signedTK.FilePath);
                string base64Data = Convert.ToBase64String(tkDaKy);
                string tenFile = signedTK.TenToKhai;

                FileToKhai ftk = new FileToKhai()
                {
                    MaToKhai = MethodLibrary.GetMaTK(tenFile),
                    MoTaToKhai = signedTK.MoTaToKhai,
                    TenFile = tenFile,
                    LoaiFile = Path.GetExtension(tenFile),
                    DoDaiFile = base64Data.Length,
                    NoiDungFile = base64Data,
                };
                listTK.Add(ftk);
            }
            ToKhais toKhais = new ToKhais()
            {
                FileToKhai = listTK.ToArray()
            };

            ThongTinDonVi donVi = new ThongTinDonVi()
            {
                TenDoiTuong = hs.tenDV,
                MaSoBHXH = hs.MDV,
                MaSoThue = hs.MST,
                LoaiDoiTuong = hs.loaiDoiTuong,
                NguoiKy = hs.nguoiKy,
                DienThoai = hs.dienThoai,
                CoQuanQuanLy = hs.maCQBHXH,
            };

            ThongTinIVAN iVAN = new ThongTinIVAN()
            {
                MaIVAN = "00040",
                TenIVAN = "Công ty THái Sơn",
            };

            ThongTinHoSo thongTinHoSo = new ThongTinHoSo()
            {
                TenThuTuc = hs.tenHS,
                MaThuTuc = hs.maNV.GetMaThuTuc(),
                KyKeKhai = DateTime.Now.ToString("MM/yyyy"),
                NgayLap = DateTime.Now.ToString("dd/MM/yyyy"),
                SoLuongFile = listTK.Count(),
                QuyTrinhISO = "",
                ToKhais = toKhais,
            };

            NoiDung noiDung = new NoiDung()
            {
                ThongTinIVAN = iVAN,
                ThongTinDonVi = donVi,
                ThongTinHoSo = thongTinHoSo
            };
            Hoso hoso = new Hoso()
            {
                NoiDung = noiDung,
            };
            MethodLibrary.SerializeToFile(hoso, pathBHXHDienTu);
        }

        public static void CreateFileHoSoDK_LanDau(this HoSoMessage hs, string pathFileHSDKLD, DataTable dtHSDK)
        {

            string HSPath = Path.Combine(Utilities.globalPath.SignedTempFolder, hs.guid);
            string pathSaveHSDK = Path.Combine(HSPath, hs.maNV);
            List<FileToKhai> listTK = new List<FileToKhai>();
            foreach (ToKhai signed in hs.toKhais)
            {
                byte[] tkDaKy = File.ReadAllBytes(signed.FilePath);
                string base64Data = Convert.ToBase64String(tkDaKy);
                string tenFile = MethodLibrary.SafeString(signed.TenToKhai);

                FileToKhai tk = new FileToKhai()
                {
                    MaToKhai = MethodLibrary.GetMaTK(tenFile),
                    MoTaToKhai = signed.MoTaToKhai,
                    TenFile = tenFile,
                    LoaiFile = Path.GetExtension(tenFile),
                    DoDaiFile = base64Data.Length,
                    NoiDungFile = base64Data,
                };
                listTK.Add(tk);
            }

            if (dtHSDK.Rows.Count == 0) return;
            DataRow row = dtHSDK.Rows[0];
            HosoDKLanDauObjSerialize hsdk = new HosoDKLanDauObjSerialize()
            {
                NoiDung = new NoiDungDK()
                {
                    TenCoQuan = row["TenCoQuan"].SafeString(),
                    MaCoQuan = row["MaCoQuan"].SafeString(),
                    LoaiDoiTuong = row["LoaiDoiTuong"].SafeString(),
                    TenDoiTuong = row["TenDoiTuong"].SafeString(),
                    MaSoThue = row["MaSoThue"].SafeString(),
                    DienThoai = row["DienThoai"].SafeString(),
                    Email = row["Email"].SafeString(),
                    NguoiLienHe = row["NguoiLienHe"].SafeString(),
                    DiaChi = row["DiaChi"].SafeString(),
                    DiaChiLienHe = row["DiaChiLienHe"].SafeString(),
                    DienThoaiLienHe = row["DienThoaiLienHe"].SafeString(),
                    NgayLap = row["NgayLap"].SafeDateTime().ToString("dd/MM/yyyy"),
                    NgayDangKy = row["NgayDangKy"].SafeDateTime().ToString("dd/MM/yyyy"),
                    PTNhanKetQua = row["PTNhanKetQua"].SafeString(),
                    ToKhais = new ToKhais()
                    {
                        FileToKhai = listTK.ToArray()
                    },
                },
            };
            MethodLibrary.SerializeToFile(hsdk, pathFileHSDKLD);
        }


        public static string GetMaThuTuc(this string tenMaHS)
        {
            return tenMaHS.Replace("-595", "").Replace("-959", "").Replace("_959", "").Replace("_595", "").Replace("_166", "").Replace("-166", "");
        }
    }
}
