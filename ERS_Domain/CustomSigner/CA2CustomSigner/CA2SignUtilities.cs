using com.itextpdf.text.pdf.security;
using ERS_Domain.CAService;
using ERS_Domain.clsUtilities;
using ERS_Domain.Model;
using iTextSharp.text.io;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.security;
using Microsoft.SqlServer.Server;
using Newtonsoft.Json;
using Org.BouncyCastle.Asn1.Pkcs;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Xml;
using System.Xml.Linq;
using VnptHashSignatures.Common;
using VnptHashSignatures.Interface;
using VnptHashSignatures.Pdf;
using static iTextSharp.text.pdf.codec.TiffWriter;

namespace ERS_Domain.CustomSigner.CA2CustomSigner
{
    public static class CA2SignUtilities
    {
        private static string xmldsigNamespaceUrl = "http://www.w3.org/2000/09/xmldsig#";
        private static string excC14NNamespaceUrl = "http://www.w3.org/2001/10/xml-exc-c14n#";
        private static string rsaSha256NamespaceUrl = "http://www.w3.org/2001/04/xmldsig-more#rsa-sha256";
        private static string sha256NamespaceUrl = "http://www.w3.org/2001/04/xmlenc#sha256";
  

        /// <summary>
        /// method nay se doc file, sau do chuan hoa tao SignedInf, tao hash cua SignedInfo roi tra ve dang base64
        /// </summary>
        /// <param name="fileType"></param>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static string CreateHashXmlToSign(XmlElement signedInfo = null)
        {
            byte[] signedInfoHash = signedInfo.Canonicalize().Hash();
            string hashToSignBase64 = signedInfoHash.ToBase64String();
            return hashToSignBase64;
        }

        public static void SetSignatureAppearance(PdfSignatureAppearance appearance, X509Certificate2 cert, string fieldName)
        {
            appearance.Reason = "Xác nhận tài liệu";
            appearance.SignatureRenderingMode = PdfSignatureAppearance.RenderingMode.DESCRIPTION;
            string subject = cert.Subject;
            string nguoiKy = subject.GetSubjectValue("CN=");
            string noiKy = subject.GetSubjectValue("S=");
            appearance.Layer2Text = $"Ngày ký: {DateTime.Now.Date} \n Người ký: {nguoiKy} \n Nơi ký: {noiKy}";
            appearance.Layer2Font = new iTextSharp.text.Font(iTextSharp.text.Font.FontFamily.HELVETICA, 10, iTextSharp.text.Font.NORMAL, iTextSharp.text.BaseColor.RED);
            var rectangle = new iTextSharp.text.Rectangle(10, 10, 250, 100);
            appearance.SetVisibleSignature(rectangle, 1, fieldName);
        }

        public static XmlElement CreateSignedInfoNode(string filePath, string xmlNodeReferencePath = "")
        {
            XmlDocument xDoc = new XmlDocument();
            xDoc.PreserveWhitespace = true;
            xDoc.Load(filePath);
            string digestBase64 = xDoc.FindNode(xmlNodeReferencePath).CreateDigestValue();
            XmlElement signedInfo = CreateSignedInfo_BHXH(digestBase64);
            return signedInfo;
        }

        /// <summary>
        /// Tao node SignedInfo cho file BHXH, chu ky so ben BHXH co refrence den toan bo file xml
        /// </summary>
        /// <param name="digestBase64">chuoi digestvalue base 64</param>
        /// <returns></returns>
        private static XmlElement CreateSignedInfo_BHXH(string digestBase64)
        {
            XmlDocument xDoc = new XmlDocument { PreserveWhitespace = true };
            XmlElement nodeSignedInfo = xDoc.CreateElement("SignedInfo");

            XmlElement nodeCanonicalizationMethod = xDoc.CreateElement("CanonicalizationMethod");
            nodeCanonicalizationMethod.SetAttribute("Algorithm", excC14NNamespaceUrl);
            nodeSignedInfo.AppendChild(nodeCanonicalizationMethod);

            XmlElement nodeSignatureMethod = xDoc.CreateElement("SignatureMethod");
            nodeSignatureMethod.SetAttribute("Algorithm", rsaSha256NamespaceUrl);
            nodeSignedInfo.AppendChild(nodeSignatureMethod);

            XmlElement nodeReference = xDoc.CreateElement("Reference");
            nodeReference.SetAttribute("URI", "");

            XmlElement nodeTransforms = xDoc.CreateElement("Transforms");
            XmlElement nodeTransform = xDoc.CreateElement("Transform");
            nodeTransform.SetAttribute("Algorithm", excC14NNamespaceUrl);
            nodeTransforms.AppendChild(nodeTransform);
            nodeReference.AppendChild(nodeTransforms);

            XmlElement nodeDigestMethod = xDoc.CreateElement("DigestMethod");
            nodeDigestMethod.SetAttribute("Algorithm", sha256NamespaceUrl);
            nodeReference.AppendChild(nodeDigestMethod);

            XmlElement nodeDigestValue = xDoc.CreateElement("DigestValue");
            nodeDigestValue.InnerText = digestBase64;
            nodeReference.AppendChild(nodeDigestValue);

            nodeSignedInfo.AppendChild(nodeReference);
            xDoc.AppendChild(nodeSignedInfo);
            return nodeSignedInfo;
        }

        private static XmlElement FindNode(this XmlDocument xDoc, string xmlNodeReferencePath)
        {
            if (xmlNodeReferencePath == "")
            {
                return xDoc.DocumentElement;
            }
            return xDoc.SelectSingleNode(xmlNodeReferencePath) as XmlElement;
        }

        private static string CreateDigestValue(this XmlElement elementToSign)
        {
            return elementToSign.Canonicalize().Hash().ToBase64String();

        }

        private static XmlDocument CreateDocumentFromElement(this XmlElement elementToSign)
        {
            var xDoc = new XmlDocument { PreserveWhitespace = true };
            xDoc.AppendChild(xDoc.ImportNode(elementToSign, true));
            return xDoc;
        }

        private static byte[] Canonicalize(this XmlElement elementToSign)
        {
            var transform = new XmlDsigExcC14NTransform();
            transform.LoadInput(elementToSign.CreateDocumentFromElement());
            using (MemoryStream ms = (MemoryStream)transform.GetOutput(typeof(Stream)))
            {
                return ms.ToArray();
            }
        }

        private static byte[] Hash(this byte[] data)
        {
            byte[] digestByte;
            using (SHA256 sha = SHA256.Create())
            {
                digestByte = sha.ComputeHash(data);
            }
            return digestByte;
        }

        public static XmlDocument CreateSigningTime(DateTime signDate, string id)
        {
            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.LoadXml($"<Object><SignatureProperties Id=\"{id}\" xmlns=\"\"><SignatureProperty Target=\"#sigid\"><SigningTime xmlns=\"http://example.org/#signatureProperties\">{signDate:yyyy-MM-dd}T{DateTime.Now:HH:mm:ss}Z</SigningTime></SignatureProperty></SignatureProperties></Object>");
            return xmlDocument;
        }

        public static XmlElement CreateSignatureNode(XmlElement signedInfo, string signatureValue, X509Certificate2 certData, string rawCertData, DateTime signTime)
        {
            XmlDocument xDoc = new XmlDocument { PreserveWhitespace = true };
            XmlElement nodeSignature = xDoc.CreateElement("Signature");
            nodeSignature.SetAttribute("Id", "sigid");
            nodeSignature.SetAttribute("xmlns", xmldsigNamespaceUrl);

            nodeSignature.AppendChild(xDoc.ImportNode(signedInfo, true));

            XmlElement nodeSignatureValue = xDoc.CreateElement("SignatureValue");
            nodeSignatureValue.InnerText = signatureValue;
            nodeSignature.AppendChild(nodeSignatureValue);

            XmlElement nodeKeyInfo = xDoc.CreateElement("KeyInfo");

            XmlElement nodeKeyValue = xDoc.CreateElement("KeyValue");
            string rsaKeyValue = certData.GetRSAPublicKey().ToXmlString(false);
            nodeKeyValue.InnerXml = rsaKeyValue;
            nodeKeyInfo.AppendChild(nodeKeyValue);
            nodeSignature.AppendChild(nodeKeyInfo);

            XmlElement nodeX509Data = xDoc.CreateElement("X509Data");
            XmlElement nodeX509SubjectName = xDoc.CreateElement("X509SubjectName");
            nodeX509SubjectName.InnerText = certData.Subject;
            nodeX509Data.AppendChild(nodeX509SubjectName);

            XmlElement nodeX509Certificate = xDoc.CreateElement("X509Certificate");
            nodeX509Certificate.InnerText = rawCertData.Replace("\r", "").Replace("\n", "");
            nodeX509Data.AppendChild(nodeX509Certificate);
            nodeSignature.AppendChild(nodeX509Data);


            XmlDocument nodeObject = CreateSigningTime(signTime, "proid");
            nodeSignature.AppendChild(xDoc.ImportNode(nodeObject.DocumentElement, true));
            return nodeSignature;
        }

        public static void AddSignatureXml(string filePath, XmlElement nodeSignedInfo, string signatureValue, string certRaw, DateTime signTime, string xPathSignNode)
        {
            X509Certificate2 cert = new X509Certificate2(Convert.FromBase64String(certRaw));
            XmlElement nodeSignature = CreateSignatureNode(nodeSignedInfo, signatureValue, cert, certRaw, signTime);
            XmlDocument xDoc = new XmlDocument { PreserveWhitespace = true };
            xDoc.Load(filePath);
            XmlNode nodeSign = xDoc.SelectSingleNode(xPathSignNode);
            nodeSign.AppendChild(xDoc.ImportNode(nodeSignature, true));
            xDoc.Save(filePath);
        }

        public static Org.BouncyCastle.X509.X509Certificate[] GetCertChain(X509Certificate2 cert)
        {
            X509Chain chain = new X509Chain();
            chain.Build(cert);
            List<Org.BouncyCastle.X509.X509Certificate> certList = new List<Org.BouncyCastle.X509.X509Certificate>();
            X509CertificateParser parser = new X509CertificateParser();
            foreach (X509ChainElement element in chain.ChainElements)
            {
                Org.BouncyCastle.X509.X509Certificate bcCert = parser.ReadCertificate(element.Certificate.RawData);
                certList.Add(bcCert);
            }
            return certList.ToArray();
        }
        public static void AddSignaturePdf(CA2PDFSignProfile profile, string outputPath, string base64SignatureValue)
        {
            byte[] cmsData = Convert.FromBase64String(base64SignatureValue);
            using (PdfReader reader = new PdfReader(profile.PDFToSign))
            using (FileStream fs = new FileStream(outputPath, FileMode.Create))
            {
                PdfPKCS7 pdfpkcs7 = new PdfPKCS7(null, profile.CertChain, "SHA-256", false);
                pdfpkcs7.SetExternalDigest(cmsData, null, "RSA");
                byte[] encodedPKCS = pdfpkcs7.GetEncodedPKCS7(profile.HashValue, null, null, null, CryptoStandard.CMS);
                IExternalSignatureContainer externalSignature = new CA2RSExternalSignatureContainer(encodedPKCS);
                MakeSignature.SignDeferred(reader, profile.Fieldname, fs, externalSignature);
            }
        }

        public static CA2PDFSignProfile CreateHashPdfToSign(string certRaw, string filePath, DateTime signDate, string fieldName = "ebhSignature1")
        {
            X509Certificate2 cert = new X509Certificate2(Convert.FromBase64String(certRaw));
            byte[] pdfUnsign = File.ReadAllBytes(filePath);
            MemoryStream ms = new MemoryStream();
            PdfReader reader = new PdfReader(filePath);
            PdfStamper stamper = PdfStamper.CreateSignature(reader, ms, '\0');
            PdfSignatureAppearance appearance = stamper.SignatureAppearance;
            SetSignatureAppearance(appearance, cert, fieldName);
            IExternalSignatureContainer empty = new ExternalBlankSignatureContainer(PdfName.ADOBE_PPKLITE, PdfName.ADBE_PKCS7_DETACHED);
            var certChain = GetCertChain(cert);
            int estimatedSize = CalculateEstimatedSignatureSize(certChain, null, null, null);
           
            PdfSignature sig = new PdfSignature(PdfName.ADOBE_PPKLITE, PdfName.ADBE_PKCS7_DETACHED)
            {
                Location = appearance.Location,
                Reason = appearance.Reason,
                Contact = appearance.Contact,
                Name = cert.Subject.GetSubjectValue("CN="),
                SignatureCreator = $"EBH CA2 Remote signing {DateTime.Now}",
                Date = new PdfDate(signDate)
            };
            appearance.CryptoDictionary = sig;
            MakeSignature.SignExternalContainer(appearance, empty, estimatedSize);

            byte[] tempdata = ms.ToArray();
            IDigest digist = DigestUtilities.GetDigest("SHA-256");
            byte[] array = new byte[8192];
            Stream rangeStream = appearance.GetRangeStream();
            int length;
            while ((length = rangeStream.Read(array, 0, array.Length)) > 0)
            {
                digist.BlockUpdate(array, 0, length);
            }
            byte[] hash1 = new byte[digist.GetDigestSize()];
            digist.DoFinal(hash1, 0);

            //PdfPKCS7 pdfpkcs7 = new PdfPKCS7(null, certChain, "SHA-256", false);
            //byte[] hash2 = pdfpkcs7.getAuthenticatedAttributeBytes(hash1, null, null, CryptoStandard.CMS);

            if (hash1 == null)
            {
                throw new Exception("Không thể hash file");
            }
            stamper.Close();
            reader.Close();
            return new CA2PDFSignProfile
            {
                PDFToSign = ms.ToArray(),
                HashValue = hash1,
                Fieldname = fieldName,
                CertChain = certChain,
            };
        }

        public static int CalculateEstimatedSignatureSize(Org.BouncyCastle.X509.X509Certificate[] certChain, ITSAClient tsc, byte[] ocsp, ICollection<byte[]> crlList)
        {
            int num = 0;
            if (certChain != null)
            {
                foreach (Org.BouncyCastle.X509.X509Certificate x509Certificate in certChain)
                {
                    num += ((x509Certificate != null) ? x509Certificate.GetEncoded().Length : 0);
                }
            }

            num += 2000;
            if (ocsp != null)
            {
                num += ocsp.Length;
            }

            if (tsc != null)
            {
                num += 4096;
            }

            if (crlList != null)
            {
                foreach (byte[] crl in crlList)
                {
                    num += ((crl != null) ? crl.Length : 0);
                }

                num += 100;
            }

            return num;
        }
    }
}
