using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERS_Domain.Request
{
    public class CA2GetCertRequest
    {
        public string sp_id { get; set; }
        public string sp_password { get; set; }
        public string user_id { get; set; }
        public string serial_number { get; set; } = "";
        public string transaction_id { get; set; }
    }

    public class CA2SignRequest
    {
        public string sp_id { get; set; }
        public string sp_password { get; set; }
        public string user_id { get; set; }
        public FileToSign[] sign_files { get; set; }
        public string transaction_id { get; set; }
        public string serial_number { get; set; }
        public string time_stamp { get; set; } 
    }

    public class FileToSign
    {
        public string data_to_be_signed { get; set; }
        public string doc_id { get; set; }
        public string file_type { get; set; }
        public string sign_type { get; set; } = "hash";
    }

    public class CA2StatusCheckRequest
    {
        public string sp_id { get; set; }
        public string sp_password { get; set; }
        public string user_id { get; set; }
        public string transaction_id { get; set; }
    }
}
