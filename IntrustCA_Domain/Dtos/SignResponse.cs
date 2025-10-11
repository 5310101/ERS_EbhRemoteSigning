using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntrustCA_Domain.Dtos
{
    public class SignResponse
    {
        public int status_code { get; set; }
        public string status { get; set; }
        public string error_desc { get; set; }
        public FileSigned[] files { get; set; }
    }
}
