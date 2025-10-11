using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntrustCA_Domain
{
    public class SessionRegisterRequest
    {
        public string user_name { get; set; }
        public string credentialID { get; set; }
        //minutes
        public int session_time { get; set; } = 8000;
        public bool is_use_request_login { get; set; } = true;
        public string auth_data { get; set; }
    }
}
