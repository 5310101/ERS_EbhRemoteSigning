using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntrustCA_Domain.Dtos
{
    public class FileSigned
    {
        public string file_id { get; set; }
        public string file_name { get; set; }
        public string content_file { get; set; }
        public string status { get; set; }
        public string error_message { get; set; }
    }
}
