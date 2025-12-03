using ERS_Domain.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI.WebControls;
using System.Xml.Serialization;
using VnptHashSignatures.Interface;

namespace ERS_Domain.Model
{
    public enum FileType
    {
        PDF = 1,
        XML = 2,
        OFFICE = 3,
    }

    public enum TrangThaiFile
    {
        TaoMoi = 6,
        KyLoi = 0,
        DaKyHash = 1,
        DangXuLy = 12,
        DaKy = 2,
        HetHan = 3,
    }

    public enum TrangThaiHoso
    {
        ChuaTaoFile =4,
        DaKyHash = 1,
        DangXuLy = 12,
        DaKy = 2,
        HetHan = 3,
        DaLayKetQua = 5,
        KyLoi = 0,
    }


    public enum RemoteSigningProvider
    {
        VNPT =1,
        VietTel =2,
        Intrust = 3,
        CA2 = 4
    }

    public class RectanglePosition
    {
        public float rx;
        public float ry;    
        public float lx;
        public float ly;

        public RectanglePosition(float rx, float ry, float lx, float ly)
        {
            this.rx = rx;
            this.ry = ry;
            this.lx = lx;
            this.ly = ly;
        }
    }


    public class ToKhaiInfo
    {
        public string MaToKhai { get; set; }
        public string TenToKhai { get; set; }
        public string TenFile { get; set; }
        public FileType Type { get; set; }
        public byte[] Data { get; set; }
        
    }

    public class DonViInfo
    {
        public string TenDonVi { get; set; }
        public string MaSoThue { get; set; }
        public string MaDonVi { get; set; }
        public string CoQuanBHXH { get; set; }
        public string NguoiKy { get; set; }
        public string DienThoai { get; set; }
        public int LoaiDoiTuong { get; set; }
    }

    public class HoSoInfo
    {
        public string GuidHS { get; set; }
        public string MaHoSo { get; set; }
        public string TenThuTuc { get; set; }
        public DateTime NgayLap {  get; set; }  
        public DonViInfo DonVi { get; set; }
        public List<ToKhaiInfo> ToKhais { get; set; }
    }

    [Serializable]
    public class SignerInfo 
    {
        public string SignerCert { get; set; }
        public byte[] UnsignData { get; set; }
        public string SigId { get; set; } = "";
        public string SigningTimeId { get; set; } = "";
        public DateTime SigningTime {  get; set; }    

    }

    public class SignedHashInfo
    {
        public IHashSigner Signer { get; set; }
        public DataSign SignData { get; set; }
        public string PathSigner { get; set; } = "";

    }

    
    public class  HoSoDKInfo
    {
        public string GuidHS { get; set; }
        public string TenHoSo { get; set; } 
        public string MaNghiepVu { get; set; }
        //neu la ho so cap ma lan dau thi object nay chua thong tin to khai DK04
        public HosoDKLanDau HoSoDK { get; set; }
        //neu la ho so cap ma lan dau thi ToKhais != null
        public List<ToKhaiInfo> ToKhais { get; set; }
    }

    public class HosoDKLanDau
    {
        public string TenCoQuan { get; set; }
        public string MaCoQuan { get; set; }
        public string LoaiDoiTuong { get; set; }
        public string TenDoiTuong { get; set; }
        public string MaSoThue { get; set; }
        public string DienThoai { get; set; }
        public string Email { get; set; }
        public string NguoiLienHe { get; set; }
        public string DiaChi { get; set; }
        public string DiaChiLienHe { get; set; }
        public string DienThoaiLienHe { get; set; }
        public DateTime NgayLap { get; set; }
        public DateTime NgayDangKy { get; set; } 
        public string MaIVan { get; set; } = "00040";
        public string TenIVan { get; set; } = "THAISON";
        public int PTNhanKetQua { get; set; }
    }
}