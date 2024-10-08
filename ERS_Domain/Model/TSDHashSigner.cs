using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VnptHashSignatures.Interface;

namespace ERS_Domain.Model
{
    public class TSDHashSigner
    {
        public string Id { get; set; }
        public string GuidHS {  get; set; } 
        public IHashSigner Signer { get; set; }
    }
}
