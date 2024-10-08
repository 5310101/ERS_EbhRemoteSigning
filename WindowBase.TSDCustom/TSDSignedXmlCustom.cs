using System.Collections;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Security.Permissions;
using System.Xml;
using Microsoft.Win32;

namespace System.Security.Cryptography.Xml
{
    [HostProtection(SecurityAction.LinkDemand, MayLeakOnAbort = true)]
    public class TSDSignedXmlCustom
    {
        private class ReferenceLevelSortOrder : IComparer
        {
            private ArrayList m_references;

            public ArrayList References
            {
                get
                {
                    return m_references;
                }
                set
                {
                    m_references = value;
                }
            }

            public int Compare(object a, object b)
            {
                ReferenceCustom referenceCustom = a as ReferenceCustom;
                ReferenceCustom referenceCustom2 = b as ReferenceCustom;
                int index = 0;
                int index2 = 0;
                int num = 0;
                foreach (ReferenceCustom reference in References)
                {
                    if (reference == referenceCustom)
                    {
                        index = num;
                    }

                    if (reference == referenceCustom2)
                    {
                        index2 = num;
                    }

                    num++;
                }

                int referenceLevel = referenceCustom.SignedXml.GetReferenceLevel(index, References);
                int referenceLevel2 = referenceCustom2.SignedXml.GetReferenceLevel(index2, References);
                return referenceLevel.CompareTo(referenceLevel2);
            }
        }

        private SignatureDescription description;

        private byte[] _digestedSignedInfo;

        private const string AllowHMACTruncationValue = "AllowHMACTruncation";

        private bool bCacheValid;

        private bool m_bResolverSet;

        private XmlDocument m_containingDocument;

        internal XmlElement m_context;

        private EncryptedXml m_exml;

        private IEnumerator m_keyInfoEnum;

        private int[] m_refLevelCache;

        private bool[] m_refProcessed;

        protected Signature m_signature;

        private AsymmetricAlgorithm m_signingKey;

        protected string m_strSigningKeyName;

        private X509Certificate2Collection m_x509Collection;

        private IEnumerator m_x509Enum;

        internal XmlResolver m_xmlResolver;

        private static bool? s_allowHmacTruncation;

        public const string XmlDecryptionTransformUrl = "http://www.w3.org/2002/07/decrypt#XML";

        public const string XmlDsigBase64TransformUrl = "http://www.w3.org/2000/09/xmldsig#base64";

        public const string XmlDsigC14NTransformUrl = "http://www.w3.org/TR/2001/REC-xml-c14n-20010315";

        public const string XmlDsigC14NWithCommentsTransformUrl = "http://www.w3.org/TR/2001/REC-xml-c14n-20010315#WithComments";

        public const string XmlDsigCanonicalizationUrl = "http://www.w3.org/TR/2001/REC-xml-c14n-20010315";

        public const string XmlDsigCanonicalizationWithCommentsUrl = "http://www.w3.org/TR/2001/REC-xml-c14n-20010315#WithComments";

        public const string XmlDsigDSAUrl = "http://www.w3.org/2000/09/xmldsig#dsa-sha1";

        public const string XmlDsigEnvelopedSignatureTransformUrl = "http://www.w3.org/2000/09/xmldsig#enveloped-signature";

        public const string XmlDsigExcC14NTransformUrl = "http://www.w3.org/2001/10/xml-exc-c14n#";

        public const string XmlDsigExcC14NWithCommentsTransformUrl = "http://www.w3.org/2001/10/xml-exc-c14n#WithComments";

        public const string XmlDsigHMACSHA1Url = "http://www.w3.org/2000/09/xmldsig#hmac-sha1";

        public const string XmlDsigMinimalCanonicalizationUrl = "http://www.w3.org/2000/09/xmldsig#minimal";

        private const string XmlDsigMoreHMACMD5Url = "http://www.w3.org/2001/04/xmldsig-more#hmac-md5";

        private const string XmlDsigMoreHMACRIPEMD160Url = "http://www.w3.org/2001/04/xmldsig-more#hmac-ripemd160";

        private const string XmlDsigMoreHMACSHA256Url = "http://www.w3.org/2001/04/xmldsig-more#hmac-sha256";

        private const string XmlDsigMoreHMACSHA384Url = "http://www.w3.org/2001/04/xmldsig-more#hmac-sha384";

        private const string XmlDsigMoreHMACSHA512Url = "http://www.w3.org/2001/04/xmldsig-more#hmac-sha512";

        public const string XmlDsigNamespaceUrl = "http://www.w3.org/2000/09/xmldsig#";

        public const string XmlDsigRSASHA1Url = "http://www.w3.org/2000/09/xmldsig#rsa-sha1";

        public const string XmlDsigSHA1Url = "http://www.w3.org/2000/09/xmldsig#sha1";

        public const string XmlDsigXPathTransformUrl = "http://www.w3.org/TR/1999/REC-xpath-19991116";

        public const string XmlDsigXsltTransformUrl = "http://www.w3.org/TR/1999/REC-xslt-19991116";

        public const string XmlLicenseTransformUrl = "urn:mpeg:mpeg21:2003:01-REL-R-NS:licenseTransform";

        private static bool AllowHmacTruncation
        {
            get
            {
                if (!s_allowHmacTruncation.HasValue)
                {
                    s_allowHmacTruncation = ReadHmacTruncationSetting();
                }

                return s_allowHmacTruncation.Value;
            }
        }

        [ComVisible(false)]
        public EncryptedXml EncryptedXml
        {
            get
            {
                if (m_exml == null)
                {
                    m_exml = new EncryptedXml(m_containingDocument);
                }

                return m_exml;
            }
            set
            {
                m_exml = value;
            }
        }

        public KeyInfo KeyInfo
        {
            get
            {
                return m_signature.KeyInfo;
            }
            set
            {
                m_signature.KeyInfo = value;
            }
        }

        [ComVisible(false)]
        public XmlResolver Resolver
        {
            set
            {
                m_xmlResolver = value;
                m_bResolverSet = true;
            }
        }

        internal bool ResolverSet => m_bResolverSet;

        public Signature Signature => m_signature;

        public string SignatureLength => m_signature.SignedInfo.SignatureLength;

        public string SignatureMethod => m_signature.SignedInfo.SignatureMethod;

        public byte[] SignatureValue => m_signature.SignatureValue;

        public SignedInfo SignedInfo => m_signature.SignedInfo;

        public AsymmetricAlgorithm SigningKey
        {
            get
            {
                return m_signingKey;
            }
            set
            {
                m_signingKey = value;
            }
        }

        public string SigningKeyName
        {
            get
            {
                return m_strSigningKeyName;
            }
            set
            {
                m_strSigningKeyName = value;
            }
        }

        public SignedXmlCustom()
        {
            Initialize(null);
        }

        public SignedXmlCustom(XmlDocument document)
        {
            if (document == null)
            {
                throw new ArgumentNullException("document");
            }

            Initialize(document.DocumentElement);
        }

        public SignedXmlCustom(XmlElement elem)
        {
            if (elem == null)
            {
                throw new ArgumentNullException("elem");
            }

            Initialize(elem);
        }

        public void AddObject(DataObjectCustom dataObject)
        {
            m_signature.AddObject(dataObject);
        }

        public void AddReference(ReferenceCustom reference)
        {
            m_signature.SignedInfo.AddReference(reference);
        }

        private X509Certificate2Collection BuildBagOfCerts()
        {
            X509Certificate2Collection x509Certificate2Collection = new X509Certificate2Collection();
            if (KeyInfo != null)
            {
                foreach (KeyInfoClause item in KeyInfo)
                {
                    if (item is KeyInfoX509Data keyInfoX509Data)
                    {
                        x509Certificate2Collection.AddRange(Utils.BuildBagOfCerts(keyInfoX509Data, CertUsageType.Verification));
                    }
                }
            }

            return x509Certificate2Collection;
        }

        private void BuildDigestedReferences()
        {
            ArrayList references = SignedInfo.References;
            m_refProcessed = new bool[references.Count];
            m_refLevelCache = new int[references.Count];
            ReferenceLevelSortOrder comparer = new ReferenceLevelSortOrder
            {
                References = references
            };
            ArrayList arrayList = new ArrayList();
            foreach (ReferenceCustom item in references)
            {
                arrayList.Add(item);
            }

            arrayList.Sort(comparer);
            CanonicalXmlNodeList canonicalXmlNodeList = new CanonicalXmlNodeList();
            foreach (DataObjectCustom @object in m_signature.ObjectList)
            {
                canonicalXmlNodeList.Add(@object.GetXml());
            }

            foreach (ReferenceCustom item2 in arrayList)
            {
                if (item2.DigestMethod == null)
                {
                    item2.DigestMethod = "http://www.w3.org/2000/09/xmldsig#sha1";
                }

                item2.UpdateHashValue(m_containingDocument, canonicalXmlNodeList);
                if (item2.Id != null)
                {
                    canonicalXmlNodeList.Add(item2.GetXml());
                }
            }
        }

        private bool CheckDigestedReferences()
        {
            ArrayList references = m_signature.SignedInfo.References;
            for (int i = 0; i < references.Count; i++)
            {
                ReferenceCustom referenceCustom = (ReferenceCustom)references[i];
                byte[] array = referenceCustom.CalculateHashValue(m_containingDocument, m_signature.ReferencedItems);
                if (array.Length != referenceCustom.DigestValue.Length)
                {
                    return false;
                }

                byte[] array2 = array;
                byte[] digestValue = referenceCustom.DigestValue;
                for (int j = 0; j < array2.Length; j++)
                {
                    if (array2[j] != digestValue[j])
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public bool CheckSignature()
        {
            bool flag = false;
            AsymmetricAlgorithm publicKey;
            do
            {
                publicKey = GetPublicKey();
                if (publicKey != null)
                {
                    flag = CheckSignature(publicKey);
                }
            }
            while (publicKey != null && !flag);
            return flag;
        }

        public bool CheckSignature(AsymmetricAlgorithm key)
        {
            if (!CheckSignedInfo(key))
            {
                return false;
            }

            return CheckDigestedReferences();
        }

        public bool CheckSignature(KeyedHashAlgorithm macAlg)
        {
            if (!DefaultSignatureFormatValidator(this))
            {
                return false;
            }

            if (!CheckSignedInfo(macAlg))
            {
                return false;
            }

            return CheckDigestedReferences();
        }

        [ComVisible(false)]
        public bool CheckSignature(X509Certificate2 certificate, bool verifySignatureOnly)
        {
            if (!CheckSignedInfo(certificate.PublicKey.Key))
            {
                return false;
            }

            if (!CheckDigestedReferences())
            {
                return false;
            }

            if (verifySignatureOnly)
            {
                return true;
            }

            X509ExtensionEnumerator enumerator = certificate.Extensions.GetEnumerator();
            while (enumerator.MoveNext())
            {
                X509Extension current = enumerator.Current;
                if (string.Compare(current.Oid.Value, "2.5.29.15", StringComparison.OrdinalIgnoreCase) == 0)
                {
                    X509KeyUsageExtension x509KeyUsageExtension = new X509KeyUsageExtension();
                    x509KeyUsageExtension.CopyFrom(current);
                    if ((x509KeyUsageExtension.KeyUsages & X509KeyUsageFlags.DigitalSignature) != 0 || (x509KeyUsageExtension.KeyUsages & X509KeyUsageFlags.NonRepudiation) != 0)
                    {
                        break;
                    }

                    return false;
                }
            }

            X509Chain x509Chain = new X509Chain();
            x509Chain.ChainPolicy.ExtraStore.AddRange(BuildBagOfCerts());
            return x509Chain.Build(certificate);
        }

        public bool CheckSignatureReturningKey(out AsymmetricAlgorithm signingKey)
        {
            bool flag = false;
            AsymmetricAlgorithm asymmetricAlgorithm = null;
            do
            {
                asymmetricAlgorithm = GetPublicKey();
                if (asymmetricAlgorithm != null)
                {
                    flag = CheckSignature(asymmetricAlgorithm);
                }
            }
            while (asymmetricAlgorithm != null && !flag);
            signingKey = asymmetricAlgorithm;
            return flag;
        }

        private bool CheckSignedInfo(AsymmetricAlgorithm key)
        {
            if (key == null)
            {
                throw new ArgumentNullException("key");
            }

            if (!(CryptoConfig.CreateFromName(SignatureMethod) is SignatureDescription signatureDescription))
            {
                throw new CryptographicException(SecurityResources.GetResourceString("Cryptography_Xml_SignatureDescriptionNotCreated"));
            }

            Type type = Type.GetType(signatureDescription.KeyAlgorithm);
            Type type2 = key.GetType();
            if ((object)type != type2 && !type.IsSubclassOf(type2) && !type2.IsSubclassOf(type))
            {
                return false;
            }

            HashAlgorithm hashAlgorithm = signatureDescription.CreateDigest();
            if (hashAlgorithm == null)
            {
                throw new CryptographicException(SecurityResources.GetResourceString("Cryptography_Xml_CreateHashAlgorithmFailed"));
            }

            byte[] c14NDigest = GetC14NDigest(hashAlgorithm);
            return signatureDescription.CreateDeformatter(key).VerifySignature(c14NDigest, m_signature.SignatureValue);
        }

        private bool CheckSignedInfo(KeyedHashAlgorithm macAlg)
        {
            if (macAlg == null)
            {
                throw new ArgumentNullException("macAlg");
            }

            int num = ((m_signature.SignedInfo.SignatureLength != null) ? Convert.ToInt32(m_signature.SignedInfo.SignatureLength, null) : macAlg.HashSize);
            if (num < 0 || num > macAlg.HashSize)
            {
                throw new CryptographicException(SecurityResources.GetResourceString("Cryptography_Xml_InvalidSignatureLength"));
            }

            if (num % 8 != 0)
            {
                throw new CryptographicException(SecurityResources.GetResourceString("Cryptography_Xml_InvalidSignatureLength2"));
            }

            if (m_signature.SignatureValue == null)
            {
                throw new CryptographicException(SecurityResources.GetResourceString("Cryptography_Xml_SignatureValueRequired"));
            }

            if (m_signature.SignatureValue.Length != num / 8)
            {
                throw new CryptographicException(SecurityResources.GetResourceString("Cryptography_Xml_InvalidSignatureLength"));
            }

            byte[] c14NDigest = GetC14NDigest(macAlg);
            for (int i = 0; i < m_signature.SignatureValue.Length; i++)
            {
                if (m_signature.SignatureValue[i] != c14NDigest[i])
                {
                    return false;
                }
            }

            return true;
        }

        public void ComputeSignature()
        {
            BuildDigestedReferences();
            AsymmetricAlgorithm signingKey = SigningKey;
            if (signingKey == null)
            {
                throw new CryptographicException(SecurityResources.GetResourceString("Cryptography_Xml_LoadKeyFailed"));
            }

            if (SignedInfo.SignatureMethod == null)
            {
                if (!(signingKey is DSA))
                {
                    if (!(signingKey is RSA))
                    {
                        throw new CryptographicException(SecurityResources.GetResourceString("Cryptography_Xml_CreatedKeyFailed"));
                    }

                    if (SignedInfo.SignatureMethod == null)
                    {
                        SignedInfo.SignatureMethod = "http://www.w3.org/2000/09/xmldsig#rsa-sha1";
                    }
                }
                else
                {
                    SignedInfo.SignatureMethod = "http://www.w3.org/2000/09/xmldsig#dsa-sha1";
                }
            }

            if (!(CryptoConfig.CreateFromName(SignedInfo.SignatureMethod) is SignatureDescription signatureDescription))
            {
                throw new CryptographicException(SecurityResources.GetResourceString("Cryptography_Xml_SignatureDescriptionNotCreated"));
            }

            HashAlgorithm hashAlgorithm = signatureDescription.CreateDigest();
            if (hashAlgorithm == null)
            {
                throw new CryptographicException(SecurityResources.GetResourceString("Cryptography_Xml_CreateHashAlgorithmFailed"));
            }

            GetC14NDigest(hashAlgorithm);
            m_signature.SignatureValue = signatureDescription.CreateFormatter(signingKey).CreateSignature(hashAlgorithm);
        }

        public byte[] CreateHash(string algorithmsSign)
        {
            BuildDigestedReferences();
            if (algorithmsSign != "DSA" && algorithmsSign != "RSA")
            {
                throw new CryptographicException(SecurityResources.GetResourceString("Cryptography_Xml_LoadKeyFailed"));
            }

            if (SignedInfo.SignatureMethod == null)
            {
                if (!(algorithmsSign == "DSA"))
                {
                    if (!(algorithmsSign == "RSA"))
                    {
                        throw new CryptographicException(SecurityResources.GetResourceString("Cryptography_Xml_CreatedKeyFailed"));
                    }

                    if (SignedInfo.SignatureMethod == null)
                    {
                        SignedInfo.SignatureMethod = "http://www.w3.org/2000/09/xmldsig#rsa-sha1";
                    }
                }
                else
                {
                    SignedInfo.SignatureMethod = "http://www.w3.org/2000/09/xmldsig#dsa-sha1";
                }
            }

            description = CryptoConfig.CreateFromName(SignedInfo.SignatureMethod) as SignatureDescription;
            if (description == null)
            {
                throw new CryptographicException(SecurityResources.GetResourceString("Cryptography_Xml_SignatureDescriptionNotCreated"));
            }

            HashAlgorithm hashAlgorithm = description.CreateDigest();
            if (hashAlgorithm == null)
            {
                throw new CryptographicException(SecurityResources.GetResourceString("Cryptography_Xml_CreateHashAlgorithmFailed"));
            }

            GetC14NDigest(hashAlgorithm);
            return hashAlgorithm.Hash;
        }

        public void SetSignature(byte[] signatureValue)
        {
            m_signature.SignatureValue = signatureValue;
        }

        public void ComputeSignature(KeyedHashAlgorithm macAlg)
        {
            if (macAlg == null)
            {
                throw new ArgumentNullException("macAlg");
            }

            if (!(macAlg is HMAC hMAC))
            {
                throw new CryptographicException(SecurityResources.GetResourceString("Cryptography_Xml_SignatureMethodKeyMismatch"));
            }

            int num = ((m_signature.SignedInfo.SignatureLength != null) ? Convert.ToInt32(m_signature.SignedInfo.SignatureLength, null) : hMAC.HashSize);
            if (num < 0 || num > hMAC.HashSize)
            {
                throw new CryptographicException(SecurityResources.GetResourceString("Cryptography_Xml_InvalidSignatureLength"));
            }

            if (num % 8 != 0)
            {
                throw new CryptographicException(SecurityResources.GetResourceString("Cryptography_Xml_InvalidSignatureLength2"));
            }

            BuildDigestedReferences();
            switch (hMAC.HashName)
            {
                case "SHA1":
                    SignedInfo.SignatureMethod = "http://www.w3.org/2000/09/xmldsig#hmac-sha1";
                    break;
                case "SHA256":
                    SignedInfo.SignatureMethod = "http://www.w3.org/2001/04/xmldsig-more#hmac-sha256";
                    break;
                case "SHA384":
                    SignedInfo.SignatureMethod = "http://www.w3.org/2001/04/xmldsig-more#hmac-sha384";
                    break;
                case "SHA512":
                    SignedInfo.SignatureMethod = "http://www.w3.org/2001/04/xmldsig-more#hmac-sha512";
                    break;
                case "MD5":
                    SignedInfo.SignatureMethod = "http://www.w3.org/2001/04/xmldsig-more#hmac-md5";
                    break;
                case "RIPEMD160":
                    SignedInfo.SignatureMethod = "http://www.w3.org/2001/04/xmldsig-more#hmac-ripemd160";
                    break;
                default:
                    throw new CryptographicException(SecurityResources.GetResourceString("Cryptography_Xml_SignatureMethodKeyMismatch"));
            }

            byte[] c14NDigest = GetC14NDigest(hMAC);
            m_signature.SignatureValue = new byte[num / 8];
            Buffer.BlockCopy(c14NDigest, 0, m_signature.SignatureValue, 0, num / 8);
        }

        private static bool DefaultSignatureFormatValidator(SignedXmlCustom signedXml)
        {
            if (!AllowHmacTruncation && signedXml.DoesSignatureUseTruncatedHmac())
            {
                return false;
            }

            return true;
        }

        private bool DoesSignatureUseTruncatedHmac()
        {
            if (SignedInfo == null || SignedInfo.SignatureLength == null)
            {
                return false;
            }

            HMAC hMAC = CryptoConfig.CreateFromName(SignatureMethod) as HMAC;
            if (hMAC == null)
            {
                if (string.Equals(SignatureMethod, "http://www.w3.org/2000/09/xmldsig#hmac-sha1", StringComparison.Ordinal))
                {
                    hMAC = new HMACSHA1();
                }
                else if (string.Equals(SignatureMethod, "http://www.w3.org/2001/04/xmldsig-more#hmac-md5", StringComparison.Ordinal))
                {
                    hMAC = new HMACMD5();
                }
            }

            if (hMAC == null)
            {
                return false;
            }

            int result = 0;
            if (!int.TryParse(SignedInfo.SignatureLength, NumberStyles.Integer, CultureInfo.InvariantCulture, out result))
            {
                return true;
            }

            int num = Math.Max(80, hMAC.HashSize / 2);
            return result < num;
        }

        private byte[] GetC14NDigest(HashAlgorithm hash)
        {
            if (!bCacheValid || !SignedInfo.CacheValid)
            {
                string text = ((m_containingDocument == null) ? null : m_containingDocument.BaseURI);
                XmlResolver xmlResolver = (m_bResolverSet ? m_xmlResolver : new XmlSecureResolver(new XmlUrlResolver(), text));
                XmlDocument xmlDocument = Utils.PreProcessElementInput(SignedInfo.GetXml(), xmlResolver, text);
                CanonicalXmlNodeList namespaces = ((m_context == null) ? null : Utils.GetPropagatedAttributes(m_context));
                Utils.AddNamespaces(xmlDocument.DocumentElement, namespaces);
                Transform canonicalizationMethodObject = SignedInfo.CanonicalizationMethodObject;
                canonicalizationMethodObject.Resolver = xmlResolver;
                canonicalizationMethodObject.BaseURI = text;
                canonicalizationMethodObject.LoadInput(xmlDocument);
                _digestedSignedInfo = canonicalizationMethodObject.GetDigestedOutput(hash);
                bCacheValid = true;
            }

            return _digestedSignedInfo;
        }

        public virtual XmlElement GetIdElement(XmlDocument document, string idValue)
        {
            if (document == null)
            {
                return null;
            }

            XmlElement elementById = document.GetElementById(idValue);
            if (elementById != null)
            {
                return elementById;
            }

            if (document.SelectSingleNode("//*[@Id=\"" + idValue + "\"]") is XmlElement result)
            {
                return result;
            }

            if (document.SelectSingleNode("//*[@id=\"" + idValue + "\"]") is XmlElement result2)
            {
                return result2;
            }

            return document.SelectSingleNode("//*[@ID=\"" + idValue + "\"]") as XmlElement;
        }

        private AsymmetricAlgorithm GetNextCertificatePublicKey()
        {
            while (m_x509Enum.MoveNext())
            {
                X509Certificate2 x509Certificate = (X509Certificate2)m_x509Enum.Current;
                if (x509Certificate != null)
                {
                    return x509Certificate.PublicKey.Key;
                }
            }

            return null;
        }

        protected virtual AsymmetricAlgorithm GetPublicKey()
        {
            if (KeyInfo == null)
            {
                throw new CryptographicException(SecurityResources.GetResourceString("Cryptography_Xml_KeyInfoRequired"));
            }

            if (m_x509Enum != null)
            {
                AsymmetricAlgorithm nextCertificatePublicKey = GetNextCertificatePublicKey();
                if (nextCertificatePublicKey != null)
                {
                    return nextCertificatePublicKey;
                }
            }

            if (m_keyInfoEnum == null)
            {
                m_keyInfoEnum = KeyInfo.GetEnumerator();
            }

            while (m_keyInfoEnum.MoveNext())
            {
                if (m_keyInfoEnum.Current is RSAKeyValue rSAKeyValue)
                {
                    return rSAKeyValue.Key;
                }

                if (m_keyInfoEnum.Current is DSAKeyValue dSAKeyValue)
                {
                    return dSAKeyValue.Key;
                }

                if (!(m_keyInfoEnum.Current is KeyInfoX509Data keyInfoX509Data))
                {
                    continue;
                }

                m_x509Collection = Utils.BuildBagOfCerts(keyInfoX509Data, CertUsageType.Verification);
                if (m_x509Collection.Count > 0)
                {
                    m_x509Enum = m_x509Collection.GetEnumerator();
                    AsymmetricAlgorithm nextCertificatePublicKey2 = GetNextCertificatePublicKey();
                    if (nextCertificatePublicKey2 != null)
                    {
                        return nextCertificatePublicKey2;
                    }
                }
            }

            return null;
        }

        private int GetReferenceLevel(int index, ArrayList references)
        {
            if (m_refProcessed[index])
            {
                return m_refLevelCache[index];
            }

            m_refProcessed[index] = true;
            ReferenceCustom referenceCustom = (ReferenceCustom)references[index];
            if (referenceCustom.Uri == null || referenceCustom.Uri.Length == 0 || (referenceCustom.Uri.Length > 0 && referenceCustom.Uri[0] != '#'))
            {
                m_refLevelCache[index] = 0;
                return 0;
            }

            if (referenceCustom.Uri.Length <= 0 || referenceCustom.Uri[0] != '#')
            {
                throw new CryptographicException(SecurityResources.GetResourceString("Cryptography_Xml_InvalidReference"));
            }

            string text = Utils.ExtractIdFromLocalUri(referenceCustom.Uri);
            if (text == "xpointer(/)")
            {
                m_refLevelCache[index] = 0;
                return 0;
            }

            for (int i = 0; i < references.Count; i++)
            {
                if (((ReferenceCustom)references[i]).Id == text)
                {
                    m_refLevelCache[index] = GetReferenceLevel(i, references) + 1;
                    return m_refLevelCache[index];
                }
            }

            m_refLevelCache[index] = 0;
            return 0;
        }

        public XmlElement GetXml()
        {
            if (m_containingDocument != null)
            {
                return m_signature.GetXml(m_containingDocument);
            }

            return m_signature.GetXml();
        }

        private void Initialize(XmlElement element)
        {
            m_containingDocument = element?.OwnerDocument;
            m_context = element;
            m_signature = new Signature();
            m_signature.SignedXml = this;
            m_signature.SignedInfo = new SignedInfo();
            m_signingKey = null;
        }

        public void LoadXml(XmlElement value)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            m_signature.LoadXml(value);
            m_context = value;
            bCacheValid = false;
        }

        [RegistryPermission(SecurityAction.Assert, Unrestricted = true)]
        private static bool ReadHmacTruncationSetting()
        {
            bool result;
            try
            {
                using RegistryKey registryKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\.NETFramework", writable: false);
                if (registryKey == null)
                {
                    return false;
                }

                object value = registryKey.GetValue("AllowHMACTruncation");
                if (value == null)
                {
                    return false;
                }

                if (registryKey.GetValueKind("AllowHMACTruncation") != RegistryValueKind.DWord)
                {
                    return false;
                }

                result = (int)value != 0;
            }
            catch (SecurityException)
            {
                result = false;
            }

            return result;
        }
    }
}

