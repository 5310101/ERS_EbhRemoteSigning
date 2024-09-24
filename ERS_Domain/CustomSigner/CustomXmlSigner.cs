using System;
using System.Collections.Generic;
using System.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Xml;
using Org.BouncyCastle.X509;
using VnptHashSignatures.Common;
using VnptHashSignatures.Interface;
using VnptHashSignatures.Xml;
using ERS_Domain.clsUtilities;

namespace ERS_Domain.CustomSigner
{
   
    public class CustomXmlSigner : BaseHashSigner, IHashSigner
    {
        private string _referenceId = "";

        private string _parentNode = "";

        private string _nameSpace = "";

        private string _nameSpaceRef = "";

        private string _signId = "signId";

        private string _signTimeId = "AddSigningTime";

        private Org.BouncyCastle.X509.X509Certificate _signer;

        private DateTime _signingTime = DateTime.UtcNow;

        private bool _addSigningTime = false;

        private XmlDocument _doc;

        public CustomXmlSigner()
        {
        }

        public void SetSignerCertificate(string certBase64)
        {
            //IL_0048: Unknown result type (might be due to invalid IL or missing references)
            //IL_004e: Expected O, but got Unknown
            if (!string.IsNullOrEmpty(certBase64))
            {
                if (certBase64.StartsWith("-----BEGIN CERTIFICATE-----"))
                {
                    certBase64 = certBase64.Replace("-----BEGIN CERTIFICATE-----", "").Replace("-----END CERTIFICATE-----", "");
                }

                _signerCert = certBase64;
                try
                {
                    X509CertificateParser val = new X509CertificateParser();
                    _signer = val.ReadCertificate(Convert.FromBase64String(certBase64));
                }
                catch (Exception)
                {
                }

                Init();
            }
        }

        public void SetUnsignData(string base64Data)
        {
            _unsignData = Convert.FromBase64String(base64Data);
        }

        public void SetSigningTime(DateTime time)
        {
            _signingTime = time;
            _addSigningTime = true;
        }

        public void SetSigningTime(DateTime time, string id)
        {
            _signingTime = time;
            _addSigningTime = true;
            _signTimeId = id;
        }

        public string SignBase64(string signedHashBase64)
        {
            byte[] array = Sign(signedHashBase64);
            if (array != null)
            {
                return Convert.ToBase64String(array);
            }

            Console.WriteLine("Error when package signed data");
            return null;
        }

        public CustomXmlSigner(byte[] unsignData, string certBase64)
            : base(unsignData, certBase64)
        {
            Init();
        }

        private void Init()
        {
            _doc = new XmlDocument();
            _signId = Guid.NewGuid().ToString();
            XmlReaderSettings settings = new XmlReaderSettings
            {
                CloseInput = true,
                IgnoreComments = false,
                IgnoreWhitespace = false,
                IgnoreProcessingInstructions = false
            };
            using (Stream input = new MemoryStream(_unsignData))
            {
                try
                {
                    XmlReader xmlReader = XmlReader.Create(input, settings);
                    _doc.Load(xmlReader);
                    xmlReader.Close();
                }
                catch (Exception ex)
                {
                    LogFile.LogToFile(ex.Message, LogFileType.MESSAGE);
                }
            }     
        }

        public bool CheckHashSignature(string signedHashBase64)
        {
            byte[] rgbSignature = Convert.FromBase64String(signedHashBase64);
            X509Certificate2 x509Certificate = new X509Certificate2(Convert.FromBase64String(_signerCert));
            RSACryptoServiceProvider rSACryptoServiceProvider = (RSACryptoServiceProvider)x509Certificate.PublicKey.Key;
            string str = _hashAlgorithm.ToString();
            return rSACryptoServiceProvider.VerifyHash(_secondHash, str, rgbSignature);
        }

        public string GetSecondHashAsBase64()
        {
            byte[] secondHashBytes = GetSecondHashBytes();
            if (secondHashBytes == null)
            {
                return null;
            }

            return Convert.ToBase64String(secondHashBytes);
        }

        public byte[] GetSecondHashBytes()
        {
            try
            {
                XmlNodeList elementsByTagName = _doc.GetElementsByTagName("Signature");
                XmlElement xmlElement = null;
                if (string.IsNullOrEmpty(_signId))
                {
                    xmlElement = (XmlElement)elementsByTagName[0];
                }
                else
                {
                    if (_signId[0] == '#')
                    {
                        _signId = _signId.Substring(1);
                    }

                    xmlElement = (XmlElement)elementsByTagName.Cast<XmlNode>().SingleOrDefault((XmlNode node) => node.Attributes["id"]?.Value == _signId || node.Attributes["Id"]?.Value == _signId);
                }

                if (xmlElement != null)
                {
                    return null;
                }

                HashAlgorithm alg = new SHA1CryptoServiceProvider();
                string hashAlg = "sha1";
                switch (_hashAlgorithm)
                {
                    case MessageDigestAlgorithm.SHA256:
                        alg = new SHA256CryptoServiceProvider();
                        hashAlg = "sha256";
                        break;
                    case MessageDigestAlgorithm.SHA384:
                        alg = new SHA384CryptoServiceProvider();
                        hashAlg = "sha384";
                        break;
                    case MessageDigestAlgorithm.SHA512:
                        alg = new SHA512CryptoServiceProvider();
                        hashAlg = "sha512";
                        break;
                }

                byte[] array = null;
                if (string.IsNullOrEmpty(_referenceId))
                {
                    array = DsigSignature.GetC14NDigest(_doc, alg);
                }
                else
                {
                    string arg = _referenceId;
                    if (_referenceId[0] == '#')
                    {
                        arg = _referenceId.Substring(1);
                    }

                    string xpath = $"//*[@Id='{arg}']";
                    XmlElement xmlElement2 = (XmlElement)_doc.SelectSingleNode(xpath);
                    if (xmlElement2 == null)
                    {
                        xpath = $"//*[@id='{arg}']";
                        xmlElement2 = (XmlElement)_doc.SelectSingleNode(xpath);
                    }

                    array = DsigSignature.GetC14NDigest(xmlElement2, _doc, alg);
                }

                string base64Digest = Convert.ToBase64String(array);
                X509Certificate2 x509Certificate = new X509Certificate2(Convert.FromBase64String(_signerCert));
                string text = "";
                text = x509Certificate.PublicKey.Key.ToXmlString(includePrivateParameters: false);
                string subjectDN = x509Certificate.SubjectName.Decode(X500DistinguishedNameFlags.None);
                XmlNode signature = CustomXmlDigSign.CreateSignature(_hashAlgorithm, _addSigningTime ? _signingTime : DateTime.MinValue, hashAlg, base64Digest, "", subjectDN, _signerCert, text, _signId, _referenceId, _signTimeId);
                DsigSignature.AddSignatureNode(_doc, signature, _parentNode, _nameSpace, _nameSpaceRef);
                return _secondHash = DsigSignature.GetHash(_doc, signature, alg);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public byte[] Sign(string signedHashBase64)
        {
            DsigSignature.AddSignatureValue(_doc.DocumentElement, signedHashBase64, _signId);
            Clear();
            XmlWriterSettings xmlWriterSettings = new XmlWriterSettings
            {
                Indent = true,
                IndentChars = "\t",
                NewLineChars = "\r\n",
                NewLineHandling = NewLineHandling.None,
                OmitXmlDeclaration = true,
                DoNotEscapeUriAttributes = false
            };
            return Encoding.UTF8.GetBytes(_doc.OuterXml);
        }

        public void SetReferenceId(string id)
        {
            _referenceId = id;
        }

        public void SetParentNodePath(string node)
        {
            _parentNode = node;
        }

        public void SetNameSpace(string nameSpace, string reference)
        {
            _nameSpace = nameSpace;
            _nameSpaceRef = reference;
        }

        public void SetSignatureID(string value)
        {
            _signId = value;
        }

        public void SetSignatureParam(XMLSignauterParam param)
        {
            if (param == null)
            {
                LogFile.LogToFile("XMLSignatureParam is null", LogFileType.MESSAGE);
                return;
            }

            if (!string.IsNullOrEmpty(param.Namespace) && !string.IsNullOrEmpty(param.NamespaceRef))
            {
                SetNameSpace(param.Namespace, param.NamespaceRef);
            }

            if (!string.IsNullOrEmpty(param.ParentNodePath))
            {
                SetParentNodePath(param.ParentNodePath);
            }

            if (!string.IsNullOrEmpty(param.ReferenceId))
            {
                SetReferenceId(param.ReferenceId);
            }

            if (!string.IsNullOrEmpty(param.SignatureId))
            {
                SetSignatureID(param.SignatureId);
            }
        }

        private void Clear()
        {
        }

        public void SetHashAlgorithm(MessageDigestAlgorithm alg)
        {
            _hashAlgorithm = alg;
        }

        public string GetSignerCertBase64()
        {
            return _signerCert;
        }

        public bool SetSignerCertchain(string pkcs7Base64)
        {
            return false;
        }

        public string GetSignerSubjectDN()
        {
            try
            {
                return ((object)_signer.SubjectDN).ToString();
            }
            catch (Exception)
            {
                return null;
            }
        }

        public SignerProfile GetSignerProfile()
        {
            byte[] array = null;
            try
            {
                array = GetSecondHashBytes();
            }
            catch (Exception)
            {
                throw;
            }

            return new SignerProfile
            {
                DocType = "XML",
                SecondHashBytes = array,
                TempData = Encoding.UTF8.GetBytes(_doc.OuterXml),
                Fieldnames = new List<string> { _signId }
            };
        }

        public byte[] Sign(SignerProfile profile, string signedHashBase64)
        {
            byte[] inArray = Convert.FromBase64String(signedHashBase64);
            _doc = new XmlDocument();
            XmlReaderSettings settings = new XmlReaderSettings
            {
                CloseInput = true,
                IgnoreComments = false,
                IgnoreWhitespace = false,
                IgnoreProcessingInstructions = true
            };
            using (Stream input = new MemoryStream(profile.TempData))
            {
                try
                {
                    XmlReader xmlReader = XmlReader.Create(input, settings);
                    _doc.Load(xmlReader);
                    xmlReader.Close();
                }
                catch (Exception)
                {
                    throw;
                }
            }

            XmlNodeList elementsByTagName = _doc.GetElementsByTagName("Signature");
            XmlElement signature = null;
            foreach (XmlElement item in elementsByTagName)
            {
                if (profile.Fieldnames.First().Equals(item.Attributes["Id"].Value))
                {
                    signature = item;
                    break;
                }
            }

            DsigSignature.AddSignatureValue(signature, Convert.ToBase64String(inArray));
            Clear();
            return Encoding.UTF8.GetBytes(_doc.OuterXml);
        }

        public bool CheckHashSignature(SignerProfile profile, string signedHashBase64)
        {
            byte[] array = Sign(profile, signedHashBase64);
            //File.WriteAllBytes("F:\\signed.xml", array);
            return MethodLibrary.VerifySignature(array, profile.DocId);
        }
    }
}
