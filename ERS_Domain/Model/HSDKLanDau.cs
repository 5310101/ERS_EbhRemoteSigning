using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace ERS_Domain.Model
{
    [XmlRoot(ElementName = "NoiDung")]
    public class NoiDungDK
    {
        [XmlElement(ElementName = "TenCoQuan")]
        public string TenCoQuan { get; set; }

        [XmlElement(ElementName = "MaCoQuan")]
        public string MaCoQuan { get; set; }

        [XmlElement(ElementName = "LoaiDoiTuong")]
        public string LoaiDoiTuong { get; set; }

        [XmlElement(ElementName = "TenDoiTuong")]
        public string TenDoiTuong { get; set; }

        [XmlElement(ElementName = "MaSoThue")]
        public string MaSoThue { get; set; }

        [XmlElement(ElementName = "DienThoai")]
        public string DienThoai { get; set; }

        [XmlElement(ElementName = "Email")]
        public string Email { get; set; }

        [XmlElement(ElementName = "NguoiLienHe")]
        public string NguoiLienHe { get; set; }

        [XmlElement(ElementName = "DiaChi")]
        public string DiaChi { get; set; }

        [XmlElement(ElementName = "DiaChiLienHe")]
        public string DiaChiLienHe { get; set; }

        [XmlElement(ElementName = "DienThoaiLienHe")]
        public string DienThoaiLienHe { get; set; }

        [XmlElement(ElementName = "NgayLap")]
        public string NgayLap { get; set; }

        [XmlElement(ElementName = "NgayDangKy")]
        public string NgayDangKy { get; set; }

        [XmlElement(ElementName = "MaIVan")]
        public string MaIVan { get; set; }

        [XmlElement(ElementName = "TenIVan")]
        public string TenIVan { get; set; }

        [XmlElement(ElementName = "PTNhanKetQua")]
        public string PTNhanKetQua { get; set; }

        [XmlElement(ElementName = "ToKhais")]
        public ToKhais ToKhais { get; set; }
    }

    [XmlRoot(ElementName = "Hoso")]
    public class HosoDKLanDauObjSerialize
    {

        [XmlElement(ElementName = "NoiDung")]
        public NoiDungDK NoiDung { get; set; }

        [XmlElement(ElementName = "CKy_Dvi")]
        public string CKyDvi { get; set; } = "";

    }
}
