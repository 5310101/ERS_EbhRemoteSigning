using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace ERS_Domain.CustomSigner.CA2CustomSigner
{
    public class CA2XMlSignerProfile
    {
        public string DocId { get; set; }   
        public string CertData { get; set; }
        public XmlElement SignedInfo { get; set; }
    }
}
