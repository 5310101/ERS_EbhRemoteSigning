using ERS_Domain.Model;

namespace ERS_Domain.Dtos
{
    public class HoSoMessage
    {
        public string guid { get; set; }
        public string uid { get; set; }
        public string serialNumber { get; set; }
        public int typeDK { get; set; }
        public ToKhai[] toKhais { get; set; }

    }
    public class ToKhai
    {
        public int Id { get; set; }
        public string GuidHS { get; set; }
        public string FilePath { get; set; }
        public FileType LoaiFile { get; set; }
        public string Uid { get; set; }
        public string SerialNumber { get; set; }
    }
}
