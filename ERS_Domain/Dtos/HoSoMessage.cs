using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERS_Domain.Dtos
{
    public class HoSoMessage
    {
        public string guid { get; set; }
        public string uid { get; set; }
        public string serialNumber { get; set; }
        public int typeDK { get; set; }
    }
}
