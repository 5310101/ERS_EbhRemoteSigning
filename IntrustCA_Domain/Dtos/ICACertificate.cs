using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntrustCA_Domain.Dtos
{
    public class ICACertificate
    {
        public string key_id { get; set; }
        public string cert_serial { get; set; }
        public string cert_valid_from { get; set; }
        public string cert_valid_to { get; set; }
        public string cert_provider { get; set; }
        public string name { get; set; }
        public string name_display { get; set; }
        public string cert_content { get; set; }
        public string device_name { get; set; }
    }
}
