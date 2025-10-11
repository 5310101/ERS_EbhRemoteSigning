using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntrustCA_Domain.Dtos
{
    public class ExtendLoginRequest
    {
        public string user_name { get; set; }
        public bool is_use_pin_code { get; set; } = false;
        public bool is_use_request_login { get; set; } = true;
        public string refresh_token { get; set; }
        public bool is_get_token_by_refresh { get; set; } = true;
        public string auth_data { get; set; }
    }
}
