using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace testSigning_Winform.Request
{
    public class ReqGetCert
    {
        public string sp_id { get; set; }
        public string sp_password { get; set; }
        public string user_id { get; set; }
        public string serial_number { get; set; }
        public string transaction_id { get; set; }
    }

    public class SignFile
    {
        public string data_to_be_signed { get; set; }
        public string doc_id { get; set; }
        public string file_type { get; set; }
        public string sign_type { get; set; }
    }

    public class ReqSign
    {
        public string sp_id { get; set; }
        public string sp_password { get; set; }
        public string user_id { get; set; }
        public string transaction_desc { get; set; }
        public string transaction_id { get; set; }
        public List<SignFile> sign_files { get; set; }
        public string serial_number { get; set; }
    }

}