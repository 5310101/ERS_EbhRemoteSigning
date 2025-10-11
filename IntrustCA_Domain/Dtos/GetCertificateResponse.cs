using IntrustCA_Domain.Dtos;
using LIB.Library;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntrustCA_Domain.Dtos
{
    public class GetCertificateResponse
    {
        public int status_code { get; set; }
        public string status { get; set; }
        public string error_desc { get; set; }
        public ICACertificate[] certificates { get; set; }
    }
}
