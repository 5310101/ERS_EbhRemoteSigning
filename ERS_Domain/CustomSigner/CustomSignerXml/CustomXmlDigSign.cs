using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.Xml;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using VnptHashSignatures.Common;

namespace ERS_Domain.CustomSigner
{
    public class CustomXmlDsigSign
    {
        public enum DsigSignatureMode
        {
            Client,
            Server
        }

        public static XmlNode CreateSignature(MessageDigestAlgorithm alg, DateTime signTime, string hashAlg, string base64Digest, string base64SignatureValue, string subjectDN, string base64Cert, string rsaKeyValue, string signatureId = "sigid", string referenceUri = "", string signTimeId = "AddSigningTime")
        {
            XmlDocument xmlDocument = new XmlDocument();
            XmlNode xmlNode = xmlDocument.CreateElement("Signature");
            XmlAttribute xmlAttribute = xmlDocument.CreateAttribute("Id");
            xmlAttribute.Value = signatureId;
            xmlNode.Attributes.Append(xmlAttribute);
            XmlAttribute xmlAttribute2 = xmlDocument.CreateAttribute("xmlns");
            xmlAttribute2.Value = "http://www.w3.org/2000/09/xmldsig#";
            xmlNode.Attributes.Append(xmlAttribute2);
            XmlNode xmlNode2 = xmlNode.AppendChild(xmlDocument.CreateElement("SignedInfo"));
            XmlNode xmlNode3 = xmlNode2.AppendChild(xmlDocument.CreateElement("CanonicalizationMethod"));
            XmlAttribute xmlAttribute3 = xmlDocument.CreateAttribute("Algorithm");
            xmlAttribute3.Value = "http://www.w3.org/TR/2001/REC-xml-c14n-20010315";
            xmlNode3.Attributes.Append(xmlAttribute3);
            XmlNode xmlNode4 = xmlNode2.AppendChild(xmlDocument.CreateElement("SignatureMethod"));
            XmlAttribute xmlAttribute4 = xmlDocument.CreateAttribute("Algorithm");
            xmlAttribute4.Value = _getSignatureAlg(alg);
            xmlNode4.Attributes.Append(xmlAttribute4);
            XmlNode xmlNode5 = xmlNode2.AppendChild(xmlDocument.CreateElement("Reference"));
            XmlAttribute xmlAttribute5 = xmlDocument.CreateAttribute("URI");
            xmlAttribute5.Value = referenceUri;
            xmlNode5.Attributes.Append(xmlAttribute5);
            XmlNode xmlNode6 = xmlNode5.AppendChild(xmlDocument.CreateElement("Transforms"));
            XmlNode xmlNode7 = xmlNode6.AppendChild(xmlDocument.CreateElement("Transform"));
            XmlAttribute xmlAttribute6 = xmlDocument.CreateAttribute("Algorithm");
            xmlAttribute6.Value = "http://www.w3.org/2000/09/xmldsig#enveloped-signature";
            xmlNode7.Attributes.Append(xmlAttribute6);
            XmlNode xmlNode8 = xmlNode5.AppendChild(xmlDocument.CreateElement("DigestMethod"));
            XmlAttribute xmlAttribute7 = xmlDocument.CreateAttribute("Algorithm");
            xmlAttribute7.Value = _getDigestMethod(alg);
            xmlNode8.Attributes.Append(xmlAttribute7);
            XmlNode xmlNode9 = xmlNode5.AppendChild(xmlDocument.CreateElement("DigestValue"));
            xmlNode9.InnerText = base64Digest;
            XmlNode xmlNode10 = xmlNode.AppendChild(xmlDocument.CreateElement("SignatureValue"));
            xmlNode10.InnerText = ((base64SignatureValue == null) ? "" : base64SignatureValue);
            XmlNode xmlNode11 = xmlNode.AppendChild(xmlDocument.CreateElement("KeyInfo"));
            XmlNode xmlNode12 = xmlNode11.AppendChild(xmlDocument.CreateElement("KeyValue"));
            xmlNode12.InnerXml = rsaKeyValue;
            XmlNode xmlNode13 = xmlNode11.AppendChild(xmlDocument.CreateElement("X509Data"));
            XmlNode xmlNode14 = xmlNode13.AppendChild(xmlDocument.CreateElement("X509SubjectName"));
            xmlNode14.InnerText = subjectDN;
            XmlNode xmlNode15 = xmlNode13.AppendChild(xmlDocument.CreateElement("X509Certificate"));
            xmlNode15.InnerText = base64Cert.Replace("\r", "").Replace("\n", "");
            if (DateTime.MinValue != signTime)
            {
                xmlDocument.AppendChild(xmlNode);
                XmlDocument xmlDocument2 = CreateSigningTime(signTime, signTimeId, signatureId);
                byte[] inArray = PerformGetDigest(xmlDocument2.DocumentElement);
                XmlNode newChild = xmlDocument.ImportNode(xmlDocument2.DocumentElement, deep: true);
                xmlDocument.GetElementsByTagName("Signature")[0].AppendChild(newChild);
                //XmlDocument xmlDocument3 = new XmlDocument();
                //xmlDocument3.LoadXml("<Reference URI=\"#" + signTimeId + "\" ><DigestMethod Algorithm=\"http://www.w3.org/2001/04/xmlenc#sha256\"/><DigestValue>" + Convert.ToBase64String(inArray) + "</DigestValue></Reference>");
                //XmlNode newChild2 = xmlDocument.ImportNode(xmlDocument3.DocumentElement, deep: true);
                //xmlDocument.GetElementsByTagName("SignedInfo")[0].AppendChild(newChild2);
            }

            return xmlDocument.AppendChild(xmlNode);
        }

        private static XmlDocument CreateSigningTime(DateTime signDate, string id, string targetId)
        {
            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.LoadXml($"<Object Id=\"{id}\" xmlns=\"\"><SignatureProperty Target=\"#sigid\"><SigningTime xmlns=\"http://example.org/#signatureProperties\">{signDate:yyyy-MM-dd}T{DateTime.Now:HH:mm:ss}Z</SigningTime></SignatureProperty></Object>");
            return xmlDocument;
        }

        private static byte[] PerformGetDigest(XmlNode xn)
        {
            //IL_0014: Unknown result type (might be due to invalid IL or missing references)
            //IL_001a: Expected O, but got Unknown
            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(xn.OuterXml);
            XmlDsigC14NTransformCustom val = new XmlDsigC14NTransformCustom();
            ((Transform)val).LoadInput((object)xmlDocument);
            Stream inputStream = (Stream)((Transform)val).GetOutput(typeof(Stream));
            SHA256 sHA = new SHA256CryptoServiceProvider();
            byte[] array = sHA.ComputeHash(inputStream);
            string text = Convert.ToBase64String(array);
            return array;
        }

        private static string _getDigestMethod(MessageDigestAlgorithm alg)
        {
            switch (alg) 
            {
                case (MessageDigestAlgorithm.SHA1):
                    return "http://www.w3.org/2000/09/xmldsig#sha1";
                case (MessageDigestAlgorithm.SHA256):
                    return "http://www.w3.org/2001/04/xmlenc#sha256";
                case (MessageDigestAlgorithm.SHA384):
                    return "http://www.w3.org/2001/04/xmldsig-more#sha384";
                case (MessageDigestAlgorithm.SHA512):
                    return "http://www.w3.org/2001/04/xmlenc#sha512";
                default:
                    return "http://www.w3.org/2001/04/xmlenc#sha256";

            };
        }

        private static string _getSignatureAlg(MessageDigestAlgorithm alg)
        {
            switch (alg)
            {
                case (MessageDigestAlgorithm.SHA1):
                    return "http://www.w3.org/2000/09/xmldsig#rsa-sha1";
                case (MessageDigestAlgorithm.SHA256):
                    return "http://www.w3.org/2001/04/xmldsig-more#rsa-sha256";
                case (MessageDigestAlgorithm.SHA384):
                    return "http://www.w3.org/2001/04/xmldsig-more#rsa-sha384";
                case (MessageDigestAlgorithm.SHA512):
                    return "http://www.w3.org/2001/04/xmldsig-more#rsa-sha512";
                default:
                    return "http://www.w3.org/2001/04/xmldsig-more#rsa-sha256";

            };
        }

        public static void AddSignatureValue(XmlElement signature, string base64Signed)
        {
            XmlNodeList elementsByTagName = signature.GetElementsByTagName("SignatureValue");
            if (elementsByTagName.Count != 1)
            {
                throw new Exception("SignatureValue tag invalid");
            }

            elementsByTagName[0].InnerText = base64Signed;
        }

        public static void AddSignatureValue(XmlElement doc, string base64Signed, string signatureId)
        {
            XmlNodeList elementsByTagName = doc.GetElementsByTagName("Signature");
            if (elementsByTagName.Count < 1)
            {
                throw new Exception("No signature tag found");
            }

            XmlElement xmlElement = null;
            if (string.IsNullOrEmpty(signatureId))
            {
                xmlElement = (XmlElement)elementsByTagName[0];
            }
            else
            {
                if (signatureId[0] == '#')
                {
                    signatureId = signatureId.Substring(1);
                }

                xmlElement = (XmlElement)elementsByTagName.Cast<XmlNode>().SingleOrDefault((XmlNode node) => node.Attributes["id"]?.Value == signatureId || node.Attributes["Id"]?.Value == signatureId);
            }

            XmlNodeList elementsByTagName2 = xmlElement.GetElementsByTagName("SignatureValue");
            if (elementsByTagName2.Count != 1)
            {
                throw new Exception("SignatureValue tag invalid");
            }

            elementsByTagName2[0].InnerText = base64Signed;
        }

        public static void AddSignatureNode(XmlDocument doc, XmlNode signature, string parentNodePath, string nameSpace, string nameSpaceRef)
        {
            XmlNode newChild = doc.ImportNode(signature, deep: true);
            if (string.IsNullOrEmpty(parentNodePath))
            {
                doc.DocumentElement.AppendChild(newChild);
                return;
            }

            XmlNamespaceManager xmlNamespaceManager = new XmlNamespaceManager(doc.NameTable);
            if (!string.IsNullOrEmpty(nameSpace) && !string.IsNullOrEmpty(nameSpaceRef))
            {
                xmlNamespaceManager.AddNamespace(nameSpace, nameSpaceRef);
            }

            XmlElement xmlElement = (XmlElement)doc.SelectSingleNode(parentNodePath, xmlNamespaceManager);
            if (xmlElement == null)
            {
                throw new Exception("No parent node in document. node name=" + parentNodePath);
            }

            xmlElement.AppendChild(newChild);
        }

        public static bool VerifySignature(byte[] signedDocBytes, string idSignature)
        {
            //IL_0029: Unknown result type (might be due to invalid IL or missing references)
            //IL_002f: Expected O, but got Unknown
            List<bool> list = new List<bool>();
            XmlDocument xmlDocument = new XmlDocument();
            try
            {
                xmlDocument.Load(new MemoryStream(signedDocBytes));
                SignedXmlCustom val = new SignedXmlCustom(xmlDocument);
                XmlNodeList elementsByTagName = xmlDocument.GetElementsByTagName("Signature");
                XmlElement xmlElement = null;
                if (string.IsNullOrEmpty(idSignature))
                {
                    xmlElement = (XmlElement)elementsByTagName[0];
                }
                else
                {
                    if (idSignature[0] == '#')
                    {
                        idSignature = idSignature.Substring(1);
                    }

                    xmlElement = (XmlElement)elementsByTagName.Cast<XmlNode>().SingleOrDefault((XmlNode node) => node.Attributes["id"].Value == idSignature);
                }

                val.LoadXml((XmlElement)elementsByTagName[0]);
                return val.CheckSignature();
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static bool VerifySignature(string XmlSignedFilePath, string idSignature)
        {
            //IL_0024: Unknown result type (might be due to invalid IL or missing references)
            //IL_002a: Expected O, but got Unknown
            List<bool> list = new List<bool>();
            XmlDocument xmlDocument = new XmlDocument();
            try
            {
                xmlDocument.Load(XmlSignedFilePath);
                SignedXmlCustom val = new SignedXmlCustom(xmlDocument);
                XmlNodeList elementsByTagName = xmlDocument.GetElementsByTagName("Signature");
                XmlElement xmlElement = null;
                if (string.IsNullOrEmpty(idSignature))
                {
                    xmlElement = (XmlElement)elementsByTagName[0];
                }
                else
                {
                    if (idSignature[0] == '#')
                    {
                        idSignature = idSignature.Substring(1);
                    }

                    xmlElement = (XmlElement)elementsByTagName.Cast<XmlNode>().SingleOrDefault((XmlNode node) => node.Attributes["id"].Value == idSignature);
                }

                val.LoadXml((XmlElement)elementsByTagName[0]);
                return val.CheckSignature();
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static byte[] GetHash(XmlDocument xdoc, XmlNode signature, HashAlgorithm alg)
        {
            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(signature.OuterXml);
            XmlNodeList elementsByTagName = xmlDocument.GetElementsByTagName("SignedInfo");
            XmlDocument xmlDocument2 = new XmlDocument();
            xmlDocument2.LoadXml(elementsByTagName[0].OuterXml);
            CustomCanonicalXmlNodeList propagatedAttributes = CustomUtils.GetPropagatedAttributes(xdoc.DocumentElement);
            CustomUtils.AddNamespaces(xmlDocument2.DocumentElement, propagatedAttributes);
            return GetC14NDigest(xmlDocument2, alg);
        }

        public static byte[] GetC14NDigest(XmlNode xn, XmlDocument doc, HashAlgorithm alg)
        {
            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(xn.OuterXml);
            CustomCanonicalXmlNodeList propagatedAttributes = CustomUtils.GetPropagatedAttributes(doc.DocumentElement);
            CustomUtils.AddNamespaces(xmlDocument.DocumentElement, propagatedAttributes);
            return GetC14NDigest(xmlDocument, alg);
        }

        public static byte[] GetC14NDigest(XmlDocument xdoc, HashAlgorithm alg)
        {
            //IL_0001: Unknown result type (might be due to invalid IL or missing references)
            //IL_0007: Expected O, but got Unknown
           
            //XmlDsigEnvelopedSignatureTransform val = new XmlDsigEnvelopedSignatureTransform();
            XmlDsigC14NTransformCustom val = new XmlDsigC14NTransformCustom(); 
            ((Transform)val).LoadInput((object)xdoc);
            return ((Transform)val).GetDigestedOutput(alg);
        }


    }
}
