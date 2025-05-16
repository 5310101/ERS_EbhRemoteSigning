using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace ERS_Domain.Model
{
    [XmlRoot(ElementName = "ThongTinIVAN")]
    public class ThongTinIVAN
    {

        [XmlElement(ElementName = "MaIVAN")]
        public string MaIVAN { get; set; }

        [XmlElement(ElementName = "TenIVAN")]
        public string TenIVAN { get; set; }
    }

    [XmlRoot(ElementName = "ThongTinDonVi")]
    public class ThongTinDonVi
    {

        [XmlElement(ElementName = "TenDoiTuong")]
        public string TenDoiTuong { get; set; }

        [XmlElement(ElementName = "MaSoBHXH")]
        public string MaSoBHXH { get; set; }

        [XmlElement(ElementName = "LoaiDoiTuong")]
        public int LoaiDoiTuong { get; set; }

        [XmlElement(ElementName = "MaSoThue")]
        public string MaSoThue { get; set; }

        [XmlElement(ElementName = "NguoiKy")]
        public string NguoiKy { get; set; }

        [XmlElement(ElementName = "DienThoai")]
        public string DienThoai { get; set; }

        [XmlElement(ElementName = "CoQuanQuanLy")]
        public string CoQuanQuanLy { get; set; }
    }

    [XmlRoot(ElementName = "FileToKhai")]
    public class FileToKhai
    {

        [XmlElement(ElementName = "MaToKhai")]
        public string MaToKhai { get; set; }

        [XmlElement(ElementName = "MoTaToKhai")]
        public string MoTaToKhai { get; set; }

        [XmlElement(ElementName = "TenFile")]
        public string TenFile { get; set; }

        [XmlElement(ElementName = "LoaiFile")]
        public string LoaiFile { get; set; }

        [XmlElement(ElementName = "DoDaiFile")]
        public int DoDaiFile { get; set; }

        [XmlElement(ElementName = "NoiDungFile")]
        public string NoiDungFile { get; set; }
    }

    [XmlRoot(ElementName = "ToKhais")]
    public class ToKhais
    {

        [XmlElement(ElementName = "FileToKhai")]
        public FileToKhai[] FileToKhai { get; set; }
    }

    [XmlRoot(ElementName = "ThongTinHoSo")]
    public class ThongTinHoSo
    {

        [XmlElement(ElementName = "TenThuTuc")]
        public string TenThuTuc { get; set; }

        [XmlElement(ElementName = "MaThuTuc")]
        public string MaThuTuc { get; set; }

        [XmlElement(ElementName = "KyKeKhai")]
        public string KyKeKhai { get; set; }

        [XmlElement(ElementName = "NgayLap")]
        public string NgayLap { get; set; }

        [XmlElement(ElementName = "SoLuongFile")]
        public int SoLuongFile { get; set; }

        [XmlElement(ElementName = "QuyTrinhISO")]
        public string QuyTrinhISO { get; set; }

        [XmlElement(ElementName = "ToKhais")]
        public ToKhais ToKhais { get; set; }
    }

    [XmlRoot(ElementName = "NoiDung")]
    public class NoiDung
    {

        [XmlElement(ElementName = "ThongTinIVAN")]
        public ThongTinIVAN ThongTinIVAN { get; set; }

        [XmlElement(ElementName = "ThongTinDonVi")]
        public ThongTinDonVi ThongTinDonVi { get; set; }

        [XmlElement(ElementName = "ThongTinHoSo")]
        public ThongTinHoSo ThongTinHoSo { get; set; }
    }

    [XmlRoot(ElementName = "Hoso")]
    public class Hoso
    {

        [XmlElement(ElementName = "NoiDung")]
        public NoiDung NoiDung { get; set; }

        [XmlElement(ElementName = "CKy_Dvi")]
        public string CKyDvi { get; set; } = "";
    }

}
