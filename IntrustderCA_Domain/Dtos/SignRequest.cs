using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntrustCA_Domain.Dtos
{
    public class SignRequest 
    {
        public FileToSignDto<FileProperties>[] files { get; set; }
        public string user_name { get; set; }
        public string credentialID { get; set; }
        public string certificate { get; set; }
        public string serial_number { get; set; }
        public bool signed_with_session { get; set; }
        public bool is_use_request_login { get; set; }
        public string auth_data { get; set; }
    }

}
