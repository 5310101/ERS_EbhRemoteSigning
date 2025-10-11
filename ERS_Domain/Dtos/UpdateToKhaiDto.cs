using ERS_Domain.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERS_Domain.Dtos
{
    public class UpdateToKhaiDto
    {
        public int Id { get; set; }
        public TrangThaiFile TrangThai { get; set; }
        public string ErrMsg { get; set; } = "";
        public DateTime LastGet { get; set; } = DateTime.Now;
    }
}
