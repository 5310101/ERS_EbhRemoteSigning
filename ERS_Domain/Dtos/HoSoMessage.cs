using ERS_Domain.Model;

namespace ERS_Domain.Dtos
{
    public class HoSoMessage
    {
        public string guid { get; set; }
        public string uid { get; set; }
        public string tenHS { get; set; }
        public string maNV { get; set; }
        public string serialNumber { get; set; }
        public TypeHS typeDK { get; set; }
        public string tenDV { get; set; }
        public string MST { get; set; }
        public string MDV { get; set; }
        public string nguoiKy { get; set; }
        public string dienThoai { get; set; }
        public string maCQBHXH { get; set; }
        public int loaiDoiTuong { get; set; }
        public ToKhai[] toKhais { get; set; }
        public string filePathHS { get; set; }  
        public string transactionId { get; set; }
    }
    public class ToKhai
    {
        public int Id { get; set; }
        public string TransactionId { get; set; }
        public string TenToKhai { get; set; }   
        public string GuidHS { get; set; }
        public string FilePath { get; set; }
        public string MoTaToKhai { get; set; }
        public FileType LoaiFile { get; set; }
    }

    public enum  TypeHS
    {
        HSNV = 0,
        HSDK = 1,
        HSDKLanDau = 2,
    }
}
