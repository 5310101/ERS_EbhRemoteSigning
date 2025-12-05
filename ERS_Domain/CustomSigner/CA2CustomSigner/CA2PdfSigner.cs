using iTextSharp.text.pdf;
using iTextSharp.text.pdf.security;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace ERS_Domain.CustomSigner.CA2CustomSigner
{
    public class CA2PDFSignProfile
    {
        public byte[] PDFToSign { get; set; }
        public byte[] HashValue { get; set; }
        public string Fieldname { get; set; }
        public Org.BouncyCastle.X509.X509Certificate[] CertChain { get; set; }
    }

    public class CA2RSExternalSignatureContainer : IExternalSignatureContainer
    {
        private byte[] _cmsSignature;
        public CA2RSExternalSignatureContainer(byte[] cmsSignature)
        {
            _cmsSignature = cmsSignature;
        }

        public byte[] Sign(Stream data)
        {
            return _cmsSignature;
        }

        public void ModifySigningDictionary(PdfDictionary signDic)
        {
            signDic.Put(PdfName.FILTER, PdfName.ADOBE_PPKLITE);
            signDic.Put(PdfName.SUBFILTER, PdfName.ADBE_PKCS7_DETACHED);
        }
    }
}
