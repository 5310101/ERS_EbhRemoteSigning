using LIB.Library;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntrustCA_Domain.Dtos
{
    public class GetCertificateRequest
    {
        public string user_id { get; set; }
        public string serial_number { get; set; }
    }
}
