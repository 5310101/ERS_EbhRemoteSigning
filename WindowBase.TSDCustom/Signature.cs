using System.Collections;
using System.Security.Permissions;
using System.Xml;

namespace System.Security.Cryptography.Xml
{
    [HostProtection(SecurityAction.LinkDemand, MayLeakOnAbort = true)]
    public class Signature
    {
        private IList m_embeddedObjects = new ArrayList();

        private string m_id;

        private KeyInfo m_keyInfo;

        private CanonicalXmlNodeList m_referencedItems = new CanonicalXmlNodeList();

        private byte[] m_signatureValue;

        private string m_signatureValueId;

        private SignedInfo m_signedInfo;

        private TSDSignedXmlCustom m_signedXml;

        public string Id
        {
            get
            {
                return m_id;
            }
            set
            {
                m_id = value;
            }
        }

        public KeyInfo KeyInfo
        {
            get
            {
                if (m_keyInfo == null)
                {
                    m_keyInfo = new KeyInfo();
                }

                return m_keyInfo;
            }
            set
            {
                m_keyInfo = value;
            }
        }

        public IList ObjectList
        {
            get
            {
                return m_embeddedObjects;
            }
            set
            {
                m_embeddedObjects = value;
            }
        }

        internal CanonicalXmlNodeList ReferencedItems => m_referencedItems;

        public byte[] SignatureValue
        {
            get
            {
                return m_signatureValue;
            }
            set
            {
                m_signatureValue = value;
            }
        }

        public SignedInfo SignedInfo
        {
            get
            {
                return m_signedInfo;
            }
            set
            {
                m_signedInfo = value;
                if (SignedXml != null && m_signedInfo != null)
                {
                    m_signedInfo.SignedXml = SignedXml;
                }
            }
        }

        internal TSDSignedXmlCustom SignedXml
        {
            get
            {
                return m_signedXml;
            }
            set
            {
                m_signedXml = value;
            }
        }

        public void AddObject(DataObjectCustom dataObject)
        {
            m_embeddedObjects.Add(dataObject);
        }

        public XmlElement GetXml1()
        {
            XmlDocument xmlDocument = new XmlDocument
            {
                PreserveWhitespace = true
            };
            XmlElement xmlElement = xmlDocument.CreateElement("Signature", "http://www.w3.org/2000/09/xmldsig#");
            if (!string.IsNullOrEmpty(m_id))
            {
                xmlElement.SetAttribute("Id", m_id);
            }

            if (m_signedInfo == null)
            {
                throw new CryptographicException(SecurityResources.GetResourceString("Cryptography_Xml_SignedInfoRequired"));
            }

            xmlElement.AppendChild(m_signedInfo.GetXml(xmlDocument));
            XmlElement xmlElement2 = xmlDocument.CreateElement("SignatureValue", "http://www.w3.org/2000/09/xmldsig#");
            if (!string.IsNullOrEmpty(m_signatureValueId))
            {
                xmlElement2.SetAttribute("Id", m_signatureValueId);
            }

            xmlElement.AppendChild(xmlElement2);
            if (KeyInfo.Count > 0)
            {
                xmlElement.AppendChild(KeyInfo.GetXml(xmlDocument));
            }

            foreach (object embeddedObject in m_embeddedObjects)
            {
                if (embeddedObject is DataObjectCustom dataObjectCustom)
                {
                    xmlElement.AppendChild(dataObjectCustom.GetXml(xmlDocument));
                }
            }

            return xmlElement;
        }

        public XmlElement GetXml()
        {
            XmlDocument document = new XmlDocument
            {
                PreserveWhitespace = true
            };
            return GetXml(document);
        }

        internal XmlElement GetXml(XmlDocument document)
        {
            XmlElement xmlElement = document.CreateElement("Signature", "http://www.w3.org/2000/09/xmldsig#");
            if (!string.IsNullOrEmpty(m_id))
            {
                xmlElement.SetAttribute("Id", m_id);
            }

            if (m_signedInfo == null)
            {
                throw new CryptographicException(SecurityResources.GetResourceString("Cryptography_Xml_SignedInfoRequired"));
            }

            xmlElement.AppendChild(m_signedInfo.GetXml(document));
            if (m_signatureValue == null)
            {
                throw new CryptographicException(SecurityResources.GetResourceString("Cryptography_Xml_SignatureValueRequired"));
            }

            XmlElement xmlElement2 = document.CreateElement("SignatureValue", "http://www.w3.org/2000/09/xmldsig#");
            xmlElement2.AppendChild(document.CreateTextNode(Convert.ToBase64String(m_signatureValue)));
            if (!string.IsNullOrEmpty(m_signatureValueId))
            {
                xmlElement2.SetAttribute("Id", m_signatureValueId);
            }

            xmlElement.AppendChild(xmlElement2);
            if (KeyInfo.Count > 0)
            {
                xmlElement.AppendChild(KeyInfo.GetXml(document));
            }

            foreach (object embeddedObject in m_embeddedObjects)
            {
                if (embeddedObject is DataObjectCustom dataObjectCustom)
                {
                    xmlElement.AppendChild(dataObjectCustom.GetXml(document));
                }
            }

            return xmlElement;
        }

        public void LoadXml(XmlElement value)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            if (!value.LocalName.Equals("Signature"))
            {
                throw new CryptographicException(SecurityResources.GetResourceString("Cryptography_Xml_InvalidElement"), "Signature");
            }

            m_id = Utils.GetAttribute(value, "Id", "http://www.w3.org/2000/09/xmldsig#");
            XmlNamespaceManager xmlNamespaceManager = new XmlNamespaceManager(value.OwnerDocument.NameTable);
            xmlNamespaceManager.AddNamespace("ds", "http://www.w3.org/2000/09/xmldsig#");
            if (!(value.SelectSingleNode("ds:SignedInfo", xmlNamespaceManager) is XmlElement value2))
            {
                throw new CryptographicException(SecurityResources.GetResourceString("Cryptography_Xml_InvalidElement"), "SignedInfo");
            }

            SignedInfo = new SignedInfo();
            SignedInfo.LoadXml(value2);
            if (!(value.SelectSingleNode("ds:SignatureValue", xmlNamespaceManager) is XmlElement xmlElement))
            {
                throw new CryptographicException(SecurityResources.GetResourceString("Cryptography_Xml_InvalidElement"), "SignedInfo/SignatureValue");
            }

            m_signatureValue = Convert.FromBase64String(Utils.DiscardWhiteSpaces(xmlElement.InnerText));
            m_signatureValueId = Utils.GetAttribute(xmlElement, "Id", "http://www.w3.org/2000/09/xmldsig#");
            XmlNodeList xmlNodeList = value.SelectNodes("ds:KeyInfo", xmlNamespaceManager);
            m_keyInfo = new KeyInfo();
            if (xmlNodeList != null)
            {
                foreach (XmlNode item in xmlNodeList)
                {
                    if (item is XmlElement value3)
                    {
                        m_keyInfo.LoadXml(value3);
                    }
                }
            }

            XmlNodeList xmlNodeList2 = value.SelectNodes("ds:Object", xmlNamespaceManager);
            m_embeddedObjects.Clear();
            if (xmlNodeList2 != null)
            {
                foreach (XmlNode item2 in xmlNodeList2)
                {
                    if (item2 is XmlElement value4)
                    {
                        DataObjectCustom dataObjectCustom = new DataObjectCustom();
                        dataObjectCustom.LoadXml(value4);
                        m_embeddedObjects.Add(dataObjectCustom);
                    }
                }
            }

            XmlNodeList xmlNodeList3 = value.SelectNodes("//*[@Id]", xmlNamespaceManager);
            if (xmlNodeList3 == null)
            {
                return;
            }

            foreach (XmlNode item3 in xmlNodeList3)
            {
                m_referencedItems.Add(item3);
            }
        }
    }
}

