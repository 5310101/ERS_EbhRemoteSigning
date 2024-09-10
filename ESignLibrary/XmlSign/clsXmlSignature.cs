// Decompiled with JetBrains decompiler
// Type: XmlSign.clsXmlSignature
// Assembly: ESignLibrary, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: FC312E54-D344-41AE-A232-7B8768ED42AA
// Assembly location: D:\1. Project\EBH\EBH\Bin\ESignLibrary.dll

using ESignLibrary;
using SignLog;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Windows.Forms;
using System.Xml;
//using XadesNetLib.XmlDsig;
//using XadesNetLib.XmlDsig.Common;

namespace XmlSign
{
    public class clsXmlSignature
    {
        public static void SignDetachedResource(string URIString, string XmlSigFileName, RSA Key)
        {
            SignedXml signedXml = new SignedXml();
            signedXml.SigningKey = (AsymmetricAlgorithm)Key;
            signedXml.AddReference(new Reference()
            {
                Uri = URIString
            });
            KeyInfo keyInfo = new KeyInfo();
            keyInfo.AddClause((KeyInfoClause)new RSAKeyValue(Key));
            signedXml.KeyInfo = keyInfo;
            signedXml.ComputeSignature();
            XmlElement xml = signedXml.GetXml();
            XmlTextWriter xmlTextWriter = new XmlTextWriter(XmlSigFileName, (Encoding)new UTF8Encoding(false));
            xml.WriteTo((XmlWriter)xmlTextWriter);
            xmlTextWriter.Close();
        }

        public static bool VerifyDetachedSignature(string XmlSigFileName)
        {
            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.Load(XmlSigFileName);
            SignedXml signedXml = new SignedXml();
            XmlNodeList elementsByTagName = xmlDocument.GetElementsByTagName("Signature");
            signedXml.LoadXml((XmlElement)elementsByTagName[0]);
            return signedXml.CheckSignature();
        }

        public static void Sign(XmlDocument document, string XmlSigFileName, RSA Key)
        {
            SignedXml signedXml = new SignedXml();
            signedXml.SigningKey = (AsymmetricAlgorithm)Key;
            signedXml.AddObject(new System.Security.Cryptography.Xml.DataObject()
            {
                Data = document.GetElementsByTagName("HSoKhaiThue"),
                Id = "signedTaxReturn"
            });
            signedXml.AddReference(new Reference()
            {
                Uri = "#signedTaxReturn"
            });
            KeyInfo keyInfo = new KeyInfo();
            keyInfo.AddClause((KeyInfoClause)new RSAKeyValue(Key));
            signedXml.KeyInfo = keyInfo;
            signedXml.ComputeSignature();
            XmlElement xml = signedXml.GetXml();
            XmlTextWriter xmlTextWriter = new XmlTextWriter(XmlSigFileName, (Encoding)new UTF8Encoding(false));
            xml.WriteTo((XmlWriter)xmlTextWriter);
            xmlTextWriter.Close();
        }

        public static XmlNode GetXmlNode(XmlDocument xDoc, string name, string xPath)
        {
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(xDoc.NameTable);
            nsmgr.AddNamespace("HoSo", name);
            if (!xPath.StartsWith("/"))
                xPath = "HoSo:" + xPath;
            return xDoc.SelectSingleNode(xPath.Replace("/", "/HoSo:"), nsmgr);
        }

        public static string Sign_EnvelopedDKTVAN(XmlDocument document, ref string xmlSigned, RSA Key, X509Certificate2 cert)
        {
            try
            {
                XmlNode xmlNode1 = clsXmlSignature.GetXmlNode(document, "http://kekhaithue.gdt.gov.vn/HSoDKy", "DKyThueDTu/DKyThue");
                document.PreserveWhitespace = true;
                SignedXml signedXml = new SignedXml(document);
                signedXml.SigningKey = (AsymmetricAlgorithm)Key;
                System.Security.Cryptography.Xml.Signature signature = signedXml.Signature;
                Reference reference = new Reference();
                if (xmlNode1.Attributes.Count == 0)
                {
                    XmlNode node = document.CreateNode(XmlNodeType.Attribute, "id", "");
                    node.Value = "ID1";
                    xmlNode1.Attributes.SetNamedItem(node);
                }
                reference.Uri = "#" + xmlNode1.Attributes[0].Value;
                XmlDsigEnvelopedSignatureTransform signatureTransform = new XmlDsigEnvelopedSignatureTransform();
                reference.AddTransform((Transform)signatureTransform);
                signature.SignedInfo.AddReference(reference);
                KeyInfo keyInfo = new KeyInfo();
                keyInfo.AddClause((KeyInfoClause)new RSAKeyValue(Key));
                KeyInfoX509Data keyInfoX509Data = new KeyInfoX509Data((X509Certificate)cert);
                keyInfoX509Data.AddSubjectName(cert.Subject);
                keyInfo.AddClause((KeyInfoClause)keyInfoX509Data);
                signature.KeyInfo = keyInfo;
                signedXml.ComputeSignature();
                XmlElement xml = signedXml.GetXml();
                new XmlNamespaceManager(document.NameTable).AddNamespace("HoSo", "http://kekhaithue.gdt.gov.vn/HSoDKy");
                XmlNodeList elementsByTagName = document.GetElementsByTagName("CKyDTu");
                if (elementsByTagName.Count == 0)
                {
                    XmlNode xmlNode2 = document.GetElementsByTagName("DKyThueDTu")[0];
                    XmlNode element = (XmlNode)document.CreateElement("CKyDTu", xmlNode2.NamespaceURI);
                    element.InnerXml = "";
                    document.GetElementsByTagName("DKyThueDTu")[0].AppendChild(element);
                    elementsByTagName = document.GetElementsByTagName("CKyDTu");
                }
                elementsByTagName[0].AppendChild((XmlNode)xml);
                xmlSigned = document.OuterXml;
                return "";
            }
            catch (Exception ex)
            {
                int num = (int)MessageBox.Show(ex.Message + ex.StackTrace);
                return ex.Message;
            }
        }

        public static string Sign_Enveloped(XmlDocument document, ref string xmlSigned, RSA Key, X509Certificate2 cert)
        {
            try
            {
                XmlNode xmlNode1 = clsXmlSignature.GetXmlNode(document, "http://kekhaithue.gdt.gov.vn/TKhaiThue", "HSoThueDTu/HSoKhaiThue");
                document.PreserveWhitespace = true;
                SignedXml signedXml = new SignedXml(document);
                signedXml.SigningKey = (AsymmetricAlgorithm)Key;
                System.Security.Cryptography.Xml.Signature signature = signedXml.Signature;
                Reference reference = new Reference();
                if (xmlNode1.Attributes.Count == 0)
                {
                    XmlNode node = document.CreateNode(XmlNodeType.Attribute, "id", "");
                    node.Value = "ID1";
                    xmlNode1.Attributes.SetNamedItem(node);
                }
                reference.Uri = "#" + xmlNode1.Attributes[0].Value;
                XmlDsigEnvelopedSignatureTransform signatureTransform = new XmlDsigEnvelopedSignatureTransform();
                reference.AddTransform((Transform)signatureTransform);
                signature.SignedInfo.AddReference(reference);
                KeyInfo keyInfo = new KeyInfo();
                keyInfo.AddClause((KeyInfoClause)new RSAKeyValue(Key));
                KeyInfoX509Data keyInfoX509Data = new KeyInfoX509Data((X509Certificate)cert);
                keyInfoX509Data.AddSubjectName(cert.Subject);
                keyInfo.AddClause((KeyInfoClause)keyInfoX509Data);
                signature.KeyInfo = keyInfo;
                signedXml.ComputeSignature();
                XmlElement xml = signedXml.GetXml();
                new XmlNamespaceManager(document.NameTable).AddNamespace("HoSo", "http://kekhaithue.gdt.gov.vn/TKhaiThue");
                XmlNodeList elementsByTagName = document.GetElementsByTagName("CKyDTu");
                if (elementsByTagName.Count == 0)
                {
                    XmlNode xmlNode2 = document.GetElementsByTagName("HSoThueDTu")[0];
                    XmlNode element = (XmlNode)document.CreateElement("CKyDTu", xmlNode2.NamespaceURI);
                    element.InnerXml = "";
                    document.GetElementsByTagName("HSoThueDTu")[0].AppendChild(element);
                    elementsByTagName = document.GetElementsByTagName("CKyDTu");
                }
                elementsByTagName[0].AppendChild((XmlNode)xml);
                xmlSigned = document.OuterXml;
                return "";
            }
            catch (Exception ex)
            {
                int num = (int)MessageBox.Show(ex.Message + ex.StackTrace);
                return ex.Message;
            }
        }

        public static string Sign_Enveloped_BH_IVAN(XmlDocument document, ref string xmlSigned, X509Certificate2 cert)
        {
            try
            {
                RSA privateKey = (RSA)cert.PrivateKey;
                document.PreserveWhitespace = true;
                SignedXml signedXml = new SignedXml(document);
                signedXml.SigningKey = (AsymmetricAlgorithm)privateKey;
                System.Security.Cryptography.Xml.Signature signature = signedXml.Signature;
                Reference reference = new Reference();
                reference.Uri = "";
                XmlDsigEnvelopedSignatureTransform signatureTransform = new XmlDsigEnvelopedSignatureTransform();
                reference.AddTransform((Transform)signatureTransform);
                signature.SignedInfo.AddReference(reference);
                KeyInfo keyInfo = new KeyInfo();
                keyInfo.AddClause((KeyInfoClause)new RSAKeyValue(privateKey));
                KeyInfoX509Data keyInfoX509Data = new KeyInfoX509Data((X509Certificate)cert);
                keyInfoX509Data.AddSubjectName(cert.Subject);
                keyInfo.AddClause((KeyInfoClause)keyInfoX509Data);
                signature.KeyInfo = keyInfo;
                signedXml.ComputeSignature();
                XmlElement xml = signedXml.GetXml();
                XmlNode newChild = clsXmlSignature.GetXmlNode(document, "", "BHXH/CKYDTU_IVAN");
                if (newChild == null)
                {
                    newChild = document.CreateNode(XmlNodeType.Element, "CKYDTU_IVAN", (string)null);
                    document.SelectSingleNode("BHXH").AppendChild(newChild);
                }
                newChild.AppendChild((XmlNode)xml);
                xmlSigned = document.OuterXml;
                return "";
            }
            catch (Exception ex)
            {
                int num = (int)MessageBox.Show(ex.Message + ex.StackTrace);
                return ex.Message;
            }
        }

        static bool isKyLoi = false;
        public static string Sign_Enveloped_BH(XmlDocument document, ref string xmlSigned, X509Certificate2 cert, string nodeKy, string sigId, string sigIdProperty, string nodeStart, bool isSHA256 = false)
        {
            XmlDocument oldDoc = document;
            try
            {
                if (isKyLoi == false)
                {
                    isSHA256 = cert.SignatureAlgorithm.FriendlyName.ToUpper().Contains("SHA256");
                }
                else
                {
                    isKyLoi = false;
                }

                if (isSHA256)
                {
                    CryptoConfig.AddAlgorithm(typeof(RSAPKCS1SHA256SignatureDescription), "http://www.w3.org/2001/04/xmldsig-more#rsa-sha256");
                }

                RSA rsa = null;
                if (cert.HasPrivateKey)
                {
                   rsa = cert.GetRSAPrivateKey() ;
                }
                else
                {
                    rsa = cert.GetRSAPublicKey() ;
                }


                //RSACryptoServiceProvider csp = null;

                //if (cert.HasPrivateKey)
                //{
                //    var exportedKey = ((RSACryptoServiceProvider)cert.PrivateKey).ToXmlString(true);
                //    csp = new RSACryptoServiceProvider(new CspParameters(24));
                //    csp.PersistKeyInCsp = false;
                //    csp.FromXmlString(exportedKey);
                //    csp = (RSACryptoServiceProvider)cert.PrivateKey;
                //}
                //else
                //{
                //    csp = (RSACryptoServiceProvider)cert.PublicKey.Key;
                //}

                if (rsa == null)
                {
                    throw new Exception("No valid cert was found!");
                }
                //}


                RSA privateKey = (RSA)cert.PrivateKey;
                document.PreserveWhitespace = true;
                SignedXml signedXml = new SignedXml(document);
                signedXml.SigningKey = rsa;


                if (isSHA256)
                {
                    signedXml.SignedInfo.SignatureMethod = "http://www.w3.org/2001/04/xmldsig-more#rsa-sha256";
                }
                System.Security.Cryptography.Xml.Signature signature = signedXml.Signature;
                signature.Id = sigId;
                Reference reference = new Reference();
                reference.Uri = "";
                XmlDsigEnvelopedSignatureTransform signatureTransform = new XmlDsigEnvelopedSignatureTransform();
                reference.AddTransform((Transform)signatureTransform);
                if (isSHA256)
                {
                    reference.DigestMethod = "http://www.w3.org/2001/04/xmlenc#sha256";
                }
                signature.SignedInfo.AddReference(reference);
                KeyInfo keyInfo = new KeyInfo();
                keyInfo.AddClause((KeyInfoClause)new RSAKeyValue(privateKey));
                KeyInfoX509Data keyInfoX509Data = new KeyInfoX509Data((X509Certificate)cert);
                keyInfoX509Data.AddSubjectName(cert.Subject);
                keyInfo.AddClause((KeyInfoClause)keyInfoX509Data);
                signature.KeyInfo = keyInfo;
                XmlElement element1 = document.CreateElement("SignatureProperties", (string)null);
                element1.SetAttribute("Id", sigIdProperty);
                System.Security.Cryptography.Xml.DataObject dataObject = new System.Security.Cryptography.Xml.DataObject();
                XmlElement element2 = document.CreateElement("SigningTime", (string)null);
                element2.SetAttribute("xmlns", "http://example.org/#signatureProperties");
                element2.InnerText = DateTime.UtcNow.ToString("s") + "Z";
                XmlElement element3 = document.CreateElement("SignatureProperty", (string)null);
                element3.SetAttribute("Target", "#" + sigId);
                element3.AppendChild((XmlNode)element2);
                element1.AppendChild((XmlNode)element3);
                dataObject.Data = element1.SelectNodes(".");
                signature.AddObject(dataObject);
                signedXml.ComputeSignature();
                XmlElement xml = signedXml.GetXml();
                XmlNode newChild = clsXmlSignature.GetXmlNode(document, "", nodeStart + "/" + nodeKy);
                if (newChild == null)
                {
                    newChild = document.CreateNode(XmlNodeType.Element, nodeKy, (string)null);
                    document.SelectSingleNode(nodeStart).AppendChild(newChild);
                }
                newChild.AppendChild((XmlNode)xml);
                xmlSigned = document.OuterXml;
                return "";
            }
            catch (Exception ex)
            {
                GhiLog("Sign_Enveloped_BH", ex, document.InnerXml);
                ErrorLog.WriteToErrorLog(ESign.fileLog, ex.Message, ex.StackTrace, document.InnerXml);
                if (ex.Message.ToUpper().Contains("INVALID ALGORITHM SPECIFIED") || ex.Message.ToUpper().Contains(("指定的算法无效").ToUpper()) ||
                    ex.Message.ToUpper().Contains("無効なアルゴリズムが指定されました") || ex.Message.ToUpper().Contains("指定的演算法無效".ToUpper()) ||
                    ex.Message.ToUpper().Contains("指定的演算法無效".ToUpper()))
                {
                    if (isSHA256)
                    {
                        isKyLoi = true;
                        return Sign_Enveloped_BH(oldDoc, ref xmlSigned, cert, nodeKy, sigId, sigIdProperty, nodeStart, false);
                    }
                    else
                    {
                        MessageBox.Show(ex.Message + Environment.NewLine + ex.StackTrace);
                    }
                }
                else if (ex.Message.ToUpper().Contains("THE PARAMETER IS INCORRECT"))
                {
                    if (isSHA256)
                    {
                        MessageBox.Show("Bạn đang sử dụng chữ ký số chuẩn SHA256, hệ thống đã tự động cấu hình lần đầu sử dụng. Vui lòng khởi động lại phần mềm để thực hiện ký số." + Environment.NewLine + ex.Message + Environment.NewLine + ex.StackTrace);
                    }
                    else
                    {
                        MessageBox.Show(ex.Message + Environment.NewLine + ex.StackTrace);
                    }
                }
                else if (ex.Message.ToUpper().Contains("INCORRECT FUNCTION"))
                {
                    return ex.Message;
                }
                else
                {

                    int num = (int)MessageBox.Show(ex.Message + Environment.NewLine + ex.StackTrace + Environment.NewLine + isSHA256.ToString());
                }
                return ex.Message;
            }
        }

        public static string Sign_Enveloped_BH_HN(XmlDocument document, ref string xmlSigned, X509Certificate2 cert, string nodeKy, string sigId, string sigIdProperty, string nodeStart, bool isSHA256 = false)
        {
            try
            {
                isSHA256 = cert.SignatureAlgorithm.FriendlyName.ToUpper().Contains("SHA256");

                if (isSHA256)
                {
                    CryptoConfig.AddAlgorithm(typeof(RSAPKCS1SHA256SignatureDescription), "http://www.w3.org/2001/04/xmldsig-more#rsa-sha256");
                }

                RSACryptoServiceProvider csp = null;
                if (cert.HasPrivateKey)
                {
                    csp = (RSACryptoServiceProvider)cert.PrivateKey;
                }
                else
                {
                    csp = (RSACryptoServiceProvider)cert.PublicKey.Key;
                }

                if (csp == null)
                {
                    throw new Exception("No valid cert was found!");
                }

                RSA privateKey = (RSA)cert.PrivateKey;
                SignedXml signedXml = new SignedXml(document);
                signedXml.SigningKey = csp;
                if (isSHA256)
                {
                    signedXml.SignedInfo.SignatureMethod = "http://www.w3.org/2001/04/xmldsig-more#rsa-sha256";
                }
                System.Security.Cryptography.Xml.Signature signature = signedXml.Signature;
                signature.Id = sigId;
                Reference reference = new Reference();
                reference.Uri = "";
                XmlDsigEnvelopedSignatureTransform signatureTransform = new XmlDsigEnvelopedSignatureTransform();
                reference.AddTransform((Transform)signatureTransform);
                if (isSHA256)
                {
                    reference.DigestMethod = "http://www.w3.org/2001/04/xmlenc#sha256";
                }
                signature.SignedInfo.AddReference(reference);
                KeyInfo keyInfo = new KeyInfo();
                keyInfo.AddClause((KeyInfoClause)new RSAKeyValue(privateKey));
                KeyInfoX509Data keyInfoX509Data = new KeyInfoX509Data((X509Certificate)cert);
                keyInfoX509Data.AddSubjectName(cert.Subject);
                keyInfo.AddClause((KeyInfoClause)keyInfoX509Data);
                signature.KeyInfo = keyInfo;
                XmlElement element1 = document.CreateElement("SignatureProperties", (string)null);
                element1.SetAttribute("Id", sigIdProperty);
                System.Security.Cryptography.Xml.DataObject dataObject = new System.Security.Cryptography.Xml.DataObject();
                XmlElement element2 = document.CreateElement("SigningTime", (string)null);
                element2.SetAttribute("xmlns", "http://example.org/#signatureProperties");
                element2.InnerText = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                XmlElement element3 = document.CreateElement("SignatureProperty", (string)null);
                element3.SetAttribute("Target", "#" + sigId);
                element3.AppendChild((XmlNode)element2);
                element1.AppendChild((XmlNode)element3);
                dataObject.Data = element1.SelectNodes(".");
                signature.AddObject(dataObject);
                signedXml.ComputeSignature();
                XmlElement xml = signedXml.GetXml();
                XmlNode newChild = clsXmlSignature.GetXmlNode(document, "", nodeStart + "/" + nodeKy);
                if (newChild == null)
                {
                    newChild = document.CreateNode(XmlNodeType.Element, nodeKy, (string)null);
                    document.SelectSingleNode(nodeStart).AppendChild(newChild);
                }
                newChild.AppendChild((XmlNode)xml);
                xmlSigned = document.OuterXml;
                return "";
            }
            catch (Exception ex)
            {
                int num = (int)MessageBox.Show(ex.Message + ex.StackTrace);
                return ex.Message;
            }
        }

        public static void CreateXML(XmlNode xsdNode, XmlElement element, ref XmlDocument xml)
        {
            if (!xsdNode.HasChildNodes)
                return;
            foreach (XmlNode childNode in xsdNode.ChildNodes)
            {
                if (childNode.Name == "xsd:element")
                {
                    XmlElement element1 = xml.CreateElement(childNode.Attributes["name"].Value);
                    clsXmlSignature.CreateXML(childNode, element1, ref xml);
                    if (element == null)
                        xml.AppendChild((XmlNode)element1);
                    else
                        element.AppendChild((XmlNode)element1);
                }
                if (childNode.Name == "xsd:attribute")
                    element.SetAttribute(childNode.Attributes["name"].Value, "");
                if (childNode.Name == "xsd:complexType" || childNode.Name == "xsd:sequence" || childNode.Name == "xsd:schema")
                    clsXmlSignature.CreateXML(childNode, element, ref xml);
            }
        }

        //public static string Sign_Enveloped_NTDT(XmlDocument document, ref string XmlSigFileName, RSA Key, X509Certificate2 cert, string SignLocation, string XPath)
        //{
        //  try
        //  {
        //    XmlDsigHelper.Sign(document).Using(cert).UsingFormat((XmlDsigSignatureFormat) 0).IncludingCertificateInSignature().IncludeTimestamp(true).NodeToSign(XPath).NodeStoreSignatureTag(SignLocation).SignToFile("temp.xml", "TimeSignatureTVAN_THAISON");
        //    XmlSigFileName = File.ReadAllText("temp.xml", Encoding.UTF8);
        //    return "";
        //  }
        //  catch (Exception ex)
        //  {
        //    return ex.Message + " " + ex.StackTrace;
        //  }
        //}

        //public static string Sign_Enveloped_NTDT_NNT(XmlDocument document, ref string XmlSigFileName, RSA Key, X509Certificate2 cert, string SignLocation, string XPath)
        //{
        //  try
        //  {
        //    XmlDsigHelper.Sign(document).Using(cert).UsingFormat((XmlDsigSignatureFormat) 0).IncludingCertificateInSignature().IncludeTimestamp(true).NodeToSign(XPath).NodeStoreSignatureTag(SignLocation).SignToFile("temp.xml", "TimeSignature");
        //    XmlSigFileName = File.ReadAllText("temp.xml");
        //    return "";
        //  }
        //  catch (Exception ex)
        //  {
        //    return ex.Message + " " + ex.StackTrace;
        //  }
        //}

        private static XmlDsigXPathTransform CreateXPathTransform(string XPathString)
        {
            XmlElement element = new XmlDocument().CreateElement((string)null, "XPath", "http://www.w3.org/2002/06/xmldsig-filter2");
            element.SetAttribute("Filter", "intersect");
            element.InnerText = XPathString;
            XmlDsigXPathTransform dsigXpathTransform = new XmlDsigXPathTransform();
            dsigXpathTransform.Algorithm = "http://www.w3.org/2002/06/xmldsig-filter2";
            dsigXpathTransform.LoadInnerXml(element.SelectNodes("."));
            return dsigXpathTransform;
        }

        // Ghi log
        public static string AppDir = Application.StartupPath;// Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
        private static string LogPath = Path.Combine(AppDir, "ErrLog.txt");
        private static string BreakLine = "==================================================";
        public static void GhiLog(string Title, string ErrContent, string PreMess = "")
        {
            try
            {
                File.AppendAllText(LogPath.Replace("ErrLog.txt", "ErrLog_" + DateTime.Now.ToString("yyyyMMdd")) + ".txt", Environment.NewLine + BreakLine + Environment.NewLine + "Title: " + Title + Environment.NewLine + "Date: " + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + Environment.NewLine + "Message: " + ErrContent + Environment.NewLine + "More Info: " + PreMess);
            }
            catch { }
        }

        public static void GhiLog(string Title, Exception ex, string PreMess = "")
        {
            try
            {
                File.AppendAllText(LogPath.Replace("ErrLog.txt", "ErrLog_" + DateTime.Now.ToString("yyyyMMdd")) + ".txt", Environment.NewLine + BreakLine + Environment.NewLine + "Title: " + Title + Environment.NewLine + "Date: " + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + Environment.NewLine + "Message: " + ex.Message + Environment.NewLine + "StackTrace: " + ex.StackTrace + ((ex.InnerException == null) ? "" : Environment.NewLine + "Inner: " + ex.InnerException) + ((PreMess == "") ? "" : Environment.NewLine + "More Info: " + PreMess));
            }
            catch { }
        }
    }
}
