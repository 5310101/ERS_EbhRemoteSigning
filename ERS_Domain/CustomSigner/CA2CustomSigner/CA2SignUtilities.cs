extern alias netSecurity;

using com.itextpdf.text.pdf.security;
using ERS_Domain.clsUtilities;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.security;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Xml;

using SignedXml = netSecurity::System.Security.Cryptography.Xml.SignedXml;
using Reference = netSecurity::System.Security.Cryptography.Xml.Reference;
using XmlDsigEnvelopedSignatureTransform = netSecurity::System.Security.Cryptography.Xml.XmlDsigEnvelopedSignatureTransform;
using XmlDsigC14NTransform = netSecurity::System.Security.Cryptography.Xml.XmlDsigC14NTransform;
using KeyInfo = netSecurity::System.Security.Cryptography.Xml.KeyInfo;
using RSAKeyValue = netSecurity::System.Security.Cryptography.Xml.RSAKeyValue;
using KeyInfoClause = netSecurity::System.Security.Cryptography.Xml.KeyInfoClause;
using Signature = netSecurity::System.Security.Cryptography.Xml.Signature;
using DataObject = netSecurity::System.Security.Cryptography.Xml.DataObject;
using KeyInfoX509Data = netSecurity::System.Security.Cryptography.Xml.KeyInfoX509Data;
using Transform = netSecurity::System.Security.Cryptography.Xml.Transform;

using System.Security.Cryptography.Xml;

namespace ERS_Domain.CustomSigner.CA2CustomSigner
{
    public static class CA2SignUtilities
    {
        private static string xmldsigNamespaceUrl = "http://www.w3.org/2000/09/xmldsig#";
        //private static string excC14NNamespaceUrl = "http://www.w3.org/2001/10/xml-exc-c14n#";
        private static string incC14NNamespaceUrl = "http://www.w3.org/TR/2001/REC-xml-c14n-20010315";
        private static string rsaSha256NamespaceUrl = "http://www.w3.org/2001/04/xmldsig-more#rsa-sha256";
        private static string sha256NamespaceUrl = "http://www.w3.org/2001/04/xmlenc#sha256";
        private static string envelopedSignatureUrl = "http://www.w3.org/2000/09/xmldsig#enveloped-signature";

        #region XML
        /// <summary>
        /// method nay se doc file, sau do chuan hoa tao SignedInfo, tao hash cua SignedInfo roi tra ve dang base64
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

        public static string ComputeHashValue(string filePath, string rawCert, out XmlDocument xDoc, string nodeTag = "")
        {
            xDoc = new XmlDocument();
            xDoc.PreserveWhitespace = true;
            xDoc.Load(filePath);
            XmlElement nodeSignedInfo = CreateSignedInfoNode(xDoc);
            XmlElement nodeSignature = CreateSignatureNode(nodeSignedInfo, rawCert, DateTime.UtcNow);

            var nodeList = xDoc.GetElementsByTagName(nodeTag);
            if (nodeList.Count == 0)
            {
                throw new Exception("Không tìm thấy node ký");
            }
            var nodeKy = nodeList[nodeList.Count - 1];
            var nodeImported = xDoc.ImportNode(nodeSignature, true);
            nodeKy.AppendChild(nodeImported);

            return nodeSignedInfo.Canonicalize().Hash().ToBase64String();
        }

        public static XmlElement CreateSignedInfoNode(XmlDocument xDoc)
        {
            XmlNamespaceManager nsm = new XmlNamespaceManager(xDoc.NameTable);
            nsm.AddNamespace("ns1", SignedXml.XmlDsigNamespaceUrl);
            XmlNode nodeSig = xDoc.SelectSingleNode("//ns1:Signature", nsm);
            nodeSig?.ParentNode.RemoveChild(nodeSig);

            XmlDsigC14NTransform transform = new XmlDsigC14NTransform();
            transform.LoadInput(xDoc);

            byte[] canonicalBytes;
            using (MemoryStream ms = new MemoryStream())
            {
                ((Stream)transform.GetOutput(typeof(Stream))).CopyTo(ms);
                canonicalBytes = ms.ToArray();
            }
            string digestValue = canonicalBytes.Hash().ToBase64String();

            XmlElement signedInfo = CreateSignedInfo_BHXH(digestValue);
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
            nodeCanonicalizationMethod.SetAttribute("Algorithm", incC14NNamespaceUrl);
            nodeSignedInfo.AppendChild(nodeCanonicalizationMethod);

            XmlElement nodeSignatureMethod = xDoc.CreateElement("SignatureMethod");
            nodeSignatureMethod.SetAttribute("Algorithm", rsaSha256NamespaceUrl);
            nodeSignedInfo.AppendChild(nodeSignatureMethod);

            XmlElement nodeReference = xDoc.CreateElement("Reference");
            nodeReference.SetAttribute("URI", "");

            XmlElement nodeTransforms = xDoc.CreateElement("Transforms");
            XmlElement nodeTransform = xDoc.CreateElement("Transform");
            nodeTransform.SetAttribute("Algorithm", envelopedSignatureUrl);
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

        private static XmlDocument CreateDocumentFromElement(this XmlElement elementToSign)
        {
            var xDoc = new XmlDocument { PreserveWhitespace = true };
            xDoc.AppendChild(xDoc.ImportNode(elementToSign, true));
            return xDoc;
        }

        private static byte[] Canonicalize(this XmlElement elementToSign)
        {
            var transform = new XmlDsigC14NTransform();
            transform.LoadInput(elementToSign.CreateDocumentFromElement());
            using (MemoryStream ms = (MemoryStream)transform.GetOutput(typeof(Stream)))
            {
                return ms.ToArray();
            }
        }

        public static byte[] GetC14NCanonicalize(this XmlNode nodeSI)
        {
            XmlDocument xDoc = new XmlDocument();
            xDoc.PreserveWhitespace = true;
            xDoc.AppendChild(xDoc.ImportNode(nodeSI, true));

            //Canonicalization
            XmlDsigC14NTransform val = new XmlDsigC14NTransform();
            val.LoadInput(xDoc);
            using (var stream = (MemoryStream)val.GetOutput(typeof(Stream)))
            {
                return stream.ToArray();
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

        public static XmlElement CreateSignatureNode(XmlElement signedInfo, string rawCertData, DateTime signTime)
        {
            X509Certificate2 certData = new X509Certificate2(Convert.FromBase64String(rawCertData));
            XmlDocument xDoc = new XmlDocument { PreserveWhitespace = true };
            XmlElement nodeSignature = xDoc.CreateElement("Signature");
            nodeSignature.SetAttribute("Id", "sigid");
            nodeSignature.SetAttribute("xmlns", xmldsigNamespaceUrl);

            nodeSignature.AppendChild(xDoc.ImportNode(signedInfo, true));

            XmlElement nodeSignatureValue = xDoc.CreateElement("SignatureValue");
            //nodeSignatureValue.InnerText = signatureValue;
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
            nodeKeyInfo.AppendChild(nodeX509Data);

            XmlDocument nodeObject = CreateSigningTime(signTime, "proid");
            nodeSignature.AppendChild(xDoc.ImportNode(nodeObject.DocumentElement, true));
            return nodeSignature;
        }

        public static void AddSignatureXml(string filePath, XmlElement nodeSignedInfo, string signatureValue, string certRaw, DateTime signTime, string xPathSignNode)
        {
            X509Certificate2 cert = new X509Certificate2(Convert.FromBase64String(certRaw));
            XmlElement nodeSignature = CreateSignatureNode(nodeSignedInfo, certRaw, signTime);
            XmlDocument xDoc = new XmlDocument { PreserveWhitespace = true };
            xDoc.Load(filePath);
            XmlNode nodeSign = xDoc.SelectSingleNode(xPathSignNode);
            nodeSign.AppendChild(xDoc.ImportNode(nodeSignature, true));
            xDoc.Save(filePath);
        }

        public static byte[] AddSignatureXmlWithData(string filePath, XmlElement nodeSignedInfo, string signatureValue, string certRaw, DateTime signTime, string xPathSignNode)
        {
            X509Certificate2 cert = new X509Certificate2(Convert.FromBase64String(certRaw));
            XmlElement nodeSignature = CreateSignatureNode(nodeSignedInfo, certRaw, signTime);
            XmlDocument xDoc = new XmlDocument { PreserveWhitespace = true };
            xDoc.Load(filePath);
            XmlNode nodeSign = xDoc.SelectSingleNode(xPathSignNode);
            nodeSign.AppendChild(xDoc.ImportNode(nodeSignature, true));
            return Encoding.UTF8.GetBytes(xDoc.OuterXml);
        }

        public static void AddSignatureToXml(string filePath, XmlDocument tempDoc, string signatureValue)
        {
            var nodeList = tempDoc.GetElementsByTagName("SignatureValue");
            if (nodeList.Count == 0)
            {
                throw new Exception("Không tìm thấy node SignatureValue");
            }
            var nodeSignatureValue = nodeList.Item(nodeList.Count - 1);
            nodeSignatureValue.InnerText = signatureValue;
            tempDoc.Save(filePath);
        }

        public static string ComputeHashValueSendToServer(string filePath, X509Certificate2 cert, string nodeSign, out string tempFile)
        {
            XmlDocument xDoc = new XmlDocument();
            xDoc.PreserveWhitespace = true;
            xDoc.Load(filePath);

            tempFile = Path.GetTempFileName();

            //Tao SignedXml
            SignedXml signedXml = new SignedXml(xDoc);
            //Tao 1 privatekey gia
            var privateKey = RSA.Create();
            signedXml.SigningKey = privateKey;
            signedXml.SignedInfo.SignatureMethod = SignedXml.XmlDsigRSASHA256Url;
            signedXml.SignedInfo.CanonicalizationMethod = SignedXml.XmlDsigC14NTransformUrl;
            Signature sig = signedXml.Signature;
            sig.Id = "sigid";
            //tao reference
            Reference reference = new Reference();
            reference.Uri = "";
            XmlDsigEnvelopedSignatureTransform transform = new XmlDsigEnvelopedSignatureTransform();
            reference.AddTransform(transform);
            reference.DigestMethod = SignedXml.XmlDsigSHA256Url;
            signedXml.AddReference(reference);

            //tao key info
            KeyInfo keyInfo = new KeyInfo();
            RSA rsa = cert.GetRSAPublicKey();
            RSAKeyValue rSAKeyValue = new RSAKeyValue(rsa);
            keyInfo.AddClause((KeyInfoClause)(object)rSAKeyValue);
            KeyInfoX509Data x509Data = new KeyInfoX509Data(cert);
            x509Data.AddSubjectName(cert.Subject);
            keyInfo.AddClause((KeyInfoClause)(object)x509Data);

            signedXml.KeyInfo = keyInfo;


            //tao Signature properties
            XmlElement ele1 = xDoc.CreateElement("SignatureProperties", (string)null);
            ele1.SetAttribute("Id", "proid");
            XmlElement ele2 = xDoc.CreateElement("SignatureProperty", (string)null);
            ele2.SetAttribute("Target", "#sigid");
            XmlElement ele3 = xDoc.CreateElement("SigningTime", (string)null);
            ele3.SetAttribute("xmlns", "http://example.org/#signatureProperties");
            ele3.InnerText = DateTime.UtcNow.ToString("s") + "Z";
            ele2.AppendChild((XmlNode)ele3);
            ele1.AppendChild((XmlNode)ele2);
            DataObject obj = new DataObject();
            obj.Data = ele1.SelectNodes(".");
            sig.AddObject(obj);

            XmlNode nodeKy = null;
            XmlNodeList nodeCkys = xDoc.GetElementsByTagName(nodeSign);
            if (nodeCkys.Count > 0)
            {
                nodeKy = nodeCkys.Item(nodeCkys.Count - 1);
            }
            if (nodeKy == null)
            {
                throw new Exception("Không tìm thấy node ký");
            }

            signedXml.ComputeSignature();
            XmlElement signatureNode = signedXml.GetXml();
            nodeKy.AppendChild(signatureNode);

            xDoc.Save(tempFile);

            //lay digest value
            var nodes = xDoc.GetElementsByTagName("SignedInfo");
            string hashValue = "";
            if (nodes.Count != 0)
            {
                var nodeSI = nodes[nodes.Count - 1];
                byte[] hash = nodeSI.GetC14NCanonicalize().Hash();
                hashValue = Convert.ToBase64String(hash);
            }

            return hashValue;
        }
 

        public static void AddSignature(string tempFile, string saveFile, string signatureValue)
        {
            using (FileStream fs = File.OpenRead(tempFile))
            {
                XmlDocument xDoc = new XmlDocument();
                xDoc.PreserveWhitespace = true;
                xDoc.Load(fs);

                //tim node signature
                XmlNodeList listNode = xDoc.GetElementsByTagName("Signature");
                if (listNode.Count == 0)
                {
                    throw new Exception("Không tìm thấy node ký");
                }
                XmlNode nodeSignature = listNode.Item(listNode.Count - 1);
                //tim node SignatureValue
                XmlNode nodeSigValue = null;
                foreach (XmlNode node in nodeSignature.ChildNodes)
                {
                    if (node.Name.Equals("SignatureValue"))
                    {
                        nodeSigValue = node;
                    }
                }
                if (nodeSigValue == null)
                {
                    throw new Exception("Không tìm thấy node SignatureValue");
                }
                nodeSigValue.InnerText = signatureValue;
                xDoc.Save(saveFile);
            }
            try
            {
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
            }
            catch (IOException ex)
            {

                throw new Exception($"Lỗi xóa file: {ex.Message}");
            }
        }

        #endregion

        #region PDF 

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
                //byte[] encodedPKCS = pdfpkcs7.GetEncodedPKCS7(profile.HashValue, null, null, null, CryptoStandard.CMS);
                //o day ko dung secondary digest vi da co san cmsData tu CA2 tra ve
                byte[] encodedPKCS = pdfpkcs7.GetEncodedPKCS7(null, null, null, null, CryptoStandard.CMS);
                IExternalSignatureContainer externalSignature = new CA2RSExternalSignatureContainer(encodedPKCS);
                MakeSignature.SignDeferred(reader, profile.Fieldname, fs, externalSignature);
            }
        }

        public static byte[] AddSignaturePdfWithData(CA2PDFSignProfile profile, string base64SignatureValue)
        {
            byte[] cmsData = Convert.FromBase64String(base64SignatureValue);
            using (PdfReader reader = new PdfReader(profile.PDFToSign))
            using (MemoryStream ms = new MemoryStream())
            {
                PdfPKCS7 pdfpkcs7 = new PdfPKCS7(null, profile.CertChain, "SHA-256", false);
                pdfpkcs7.SetExternalDigest(cmsData, null, "RSA");
                byte[] encodedPKCS = pdfpkcs7.GetEncodedPKCS7(null, null, null, null, CryptoStandard.CMS);
                IExternalSignatureContainer externalSignature = new CA2RSExternalSignatureContainer(encodedPKCS);
                MakeSignature.SignDeferred(reader, profile.Fieldname, ms, externalSignature);
                return ms.ToArray();
            }
        }

        public static CA2PDFSignProfile CreateHashPdfToSign(string certRaw, string filePath, DateTime signDate, string transactionId, string docId, string fieldName = "ebhSignature1")
        {
            X509Certificate2 cert = new X509Certificate2(Convert.FromBase64String(certRaw));
            var certChain = GetCertChain(cert);
            MemoryStream ms = new MemoryStream();
            PdfReader reader = new PdfReader(filePath);
            PdfStamper stamper = PdfStamper.CreateSignature(reader, ms, '\0');
            PdfSignatureAppearance appearance = stamper.SignatureAppearance;
            SetSignatureAppearance(appearance, cert, fieldName);
            IExternalSignatureContainer empty = new ExternalBlankSignatureContainer(PdfName.ADOBE_PPKLITE, PdfName.ADBE_PKCS7_DETACHED);

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
            //CA2 ko can dung authenticated attribute
            //PdfPKCS7 pdfpkcs7 = new PdfPKCS7(null, certChain, "SHA-256", false);
            //byte[] hash2 = pdfpkcs7.getAuthenticatedAttributeBytes(hash1, null, null, CryptoStandard.CMS);
            stamper.Close();
            reader.Close();
            if (hash1.Length == 0)
            {
                throw new Exception("Không thể hash file");
            }

            return new CA2PDFSignProfile
            {
                PDFToSign = tempdata,
                HashValue = hash1,
                Fieldname = fieldName,
                CertChain = certChain,
                DocId = docId,
                TransactionId = transactionId,
            };
        }
        public static void SetSignatureAppearance(PdfSignatureAppearance appearance, X509Certificate2 cert, string fieldName)
        {
            appearance.Reason = "Xác nhận tài liệu";
            appearance.SignatureRenderingMode = PdfSignatureAppearance.RenderingMode.DESCRIPTION;
            string subject = cert.Subject;
            string nguoiKy = subject.GetSubjectValue("CN=");
            string noiKy = subject.GetSubjectValue("S=");
            appearance.Layer2Text = $"Ngày ký: {DateTime.Now} \nNgười ký: {nguoiKy} \nNơi ký: {noiKy}";
            BaseFont fontBase = BaseFont.CreateFont(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "times.ttf"), BaseFont.IDENTITY_H, BaseFont.EMBEDDED);
            var itextFont = new iTextSharp.text.Font(fontBase, 10, iTextSharp.text.Font.NORMAL, iTextSharp.text.BaseColor.RED);
            appearance.Layer2Font = new iTextSharp.text.Font(itextFont);
            var rectangle = new iTextSharp.text.Rectangle(10, 10, 250, 100);
            appearance.SetVisibleSignature(rectangle, 1, fieldName);
        }

        //CA2 chi ky hash1 (PKCS#1)
        public static bool ValidSignaturePDF(string base64StringValue, byte[] hashValue, X509Certificate2 cert)
        {
            using (var rsa = cert.GetRSAPublicKey())
            {
                return rsa.VerifyHash(hashValue, Convert.FromBase64String(base64StringValue), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            }
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
        #endregion
    }
}
