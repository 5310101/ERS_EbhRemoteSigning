using ERS_Domain.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERS_Domain.Dtos
{
    public class UpdateHoSoDto
    {
        public string[] ListId { get; set; }
        public TrangThaiHoso TrangThai  { get; set; }
        public DateTime LastGet { get; set; } = DateTime.Now;
        public string ErrMsg { get; set; } = "";
        public string FilePath { get; set; } = "";
    }
}
