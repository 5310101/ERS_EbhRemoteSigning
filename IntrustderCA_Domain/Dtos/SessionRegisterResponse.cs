using LIB.Library;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace IntrustCA_Domain
{
    public class SessionRegisterResponse
    {
        public int status_code { get; set; }
        public string error_desc { get; set; }
        public string status { get; set; }
        public string auth_data { get; set; }
        public string refresh_token { get; set; }
    }
}
