using System;
using System.Globalization;
using System.Xml;
using MS.Internal.WindowsBase;

namespace MS.Internal.IO.Packaging
{

    internal static class XmlSignatureProperties
    {
        private struct TimeFormatMapEntry
        {
            private string _xmlFormatString;

            private string[] _dateTimePatterns;

            public string Format => _xmlFormatString;

            public string[] Patterns => _dateTimePatterns;

            public TimeFormatMapEntry(string xmlFormatString, string[] dateTimePatterns)
            {
                _xmlFormatString = xmlFormatString;
                _dateTimePatterns = dateTimePatterns;
            }
        }

        private static readonly TimeFormatMapEntry[] _dateTimePatternMap = new TimeFormatMapEntry[6]
        {
        new TimeFormatMapEntry("YYYY-MM-DDThh:mm:ss.sTZD", new string[2] { "yyyy-MM-ddTHH:mm:ss.fzzz", "yyyy-MM-ddTHH:mm:ss.fZ" }),
        new TimeFormatMapEntry("YYYY-MM-DDThh:mm:ssTZD", new string[2] { "yyyy-MM-ddTHH:mm:sszzz", "yyyy-MM-ddTHH:mm:ssZ" }),
        new TimeFormatMapEntry("YYYY-MM-DDThh:mmTZD", new string[2] { "yyyy-MM-ddTHH:mmzzz", "yyyy-MM-ddTHH:mmZ" }),
        new TimeFormatMapEntry("YYYY-MM-DD", new string[1] { "yyyy-MM-dd" }),
        new TimeFormatMapEntry("YYYY-MM", new string[1] { "yyyy-MM" }),
        new TimeFormatMapEntry("YYYY", new string[1] { "yyyy" })
        };

        internal static string DefaultDateTimeFormat => _dateTimePatternMap[0].Format;

        internal static bool LegalFormat(string candidateFormat)
        {
            if (candidateFormat == null)
            {
                throw new ArgumentNullException("candidateFormat");
            }

            return GetIndex(candidateFormat) != -1;
        }

        internal static XmlElement AssembleSignatureProperties(XmlDocument xDoc, DateTime dateTime, string xmlDateTimeFormat, string signatureId)
        {
            Invariant.Assert(xDoc != null);
            Invariant.Assert(signatureId != null);
            if (xmlDateTimeFormat == null)
            {
                xmlDateTimeFormat = DefaultDateTimeFormat;
            }

            string[] array = ConvertXmlFormatStringToDateTimeFormatString(xmlDateTimeFormat);
            XmlElement xmlElement = xDoc.CreateElement(XTable.Get(XTable.ID.SignaturePropertiesTagName), "http://www.w3.org/2000/09/xmldsig#");
            XmlElement xmlElement2 = xDoc.CreateElement(XTable.Get(XTable.ID.SignaturePropertyTagName), "http://www.w3.org/2000/09/xmldsig#");
            xmlElement.AppendChild(xmlElement2);
            XmlAttribute xmlAttribute = xDoc.CreateAttribute(XTable.Get(XTable.ID.SignaturePropertyIdAttrName));
            xmlAttribute.Value = XTable.Get(XTable.ID.SignaturePropertyIdAttrValue);
            xmlElement2.Attributes.Append(xmlAttribute);
            XmlAttribute xmlAttribute2 = xDoc.CreateAttribute(XTable.Get(XTable.ID.TargetAttrName));
            xmlAttribute2.Value = "#" + signatureId;
            xmlElement2.Attributes.Append(xmlAttribute2);
            XmlElement xmlElement3 = xDoc.CreateElement(XTable.Get(XTable.ID.SignatureTimeTagName), XTable.Get(XTable.ID.OpcSignatureNamespace));
            XmlElement xmlElement4 = xDoc.CreateElement(XTable.Get(XTable.ID.SignatureTimeFormatTagName), XTable.Get(XTable.ID.OpcSignatureNamespace));
            XmlElement xmlElement5 = xDoc.CreateElement(XTable.Get(XTable.ID.SignatureTimeValueTagName), XTable.Get(XTable.ID.OpcSignatureNamespace));
            xmlElement4.AppendChild(xDoc.CreateTextNode(xmlDateTimeFormat));
            xmlElement5.AppendChild(xDoc.CreateTextNode(DateTimeToXmlFormattedTime(dateTime, array[0])));
            xmlElement3.AppendChild(xmlElement4);
            xmlElement3.AppendChild(xmlElement5);
            xmlElement2.AppendChild(xmlElement3);
            return xmlElement;
        }

        internal static DateTime ParseSigningTime(XmlReader reader, string signatureId, out string timeFormat)
        {
            if (reader == null)
            {
                throw new ArgumentNullException("reader");
            }

            bool flag = false;
            bool flag2 = false;
            string strB = "http://www.w3.org/2000/09/xmldsig#";
            string text = XTable.Get(XTable.ID.SignaturePropertyTagName);
            string strA = XTable.Get(XTable.ID.SignaturePropertiesTagName);
            DateTime result = DateTime.Now;
            timeFormat = null;
            while (reader.Read())
            {
                if (reader.MoveToContent() == XmlNodeType.Element && string.CompareOrdinal(reader.NamespaceURI, strB) == 0 && string.CompareOrdinal(reader.LocalName, text) == 0 && reader.Depth == 2)
                {
                    if (VerifyIdAttribute(reader))
                    {
                        if (flag2)
                        {
                            throw new XmlException(SR.Get("PackageSignatureCorruption"));
                        }

                        flag2 = true;
                        if (VerifyTargetAttribute(reader, signatureId))
                        {
                            result = ParseSignatureTimeTag(reader, out timeFormat);
                            flag = true;
                        }
                    }
                }
                else if ((string.CompareOrdinal(text, reader.LocalName) != 0 || reader.NodeType != XmlNodeType.EndElement) && reader.Depth <= 2)
                {
                    if (string.CompareOrdinal(strA, reader.LocalName) == 0 && reader.NodeType == XmlNodeType.EndElement)
                    {
                        break;
                    }

                    throw new XmlException(SR.Get("RequiredTagNotFound", text));
                }
            }

            if (!flag)
            {
                throw new XmlException(SR.Get("PackageSignatureCorruption"));
            }

            return result;
        }

        private static DateTime ParseSignatureTimeTag(XmlReader reader, out string timeFormat)
        {
            int num = 0;
            string strB = XTable.Get(XTable.ID.OpcSignatureNamespace);
            string strA = XTable.Get(XTable.ID.SignaturePropertyTagName);
            string text = XTable.Get(XTable.ID.SignatureTimeTagName);
            string strB2 = XTable.Get(XTable.ID.SignatureTimeValueTagName);
            string strB3 = XTable.Get(XTable.ID.SignatureTimeFormatTagName);
            timeFormat = null;
            string text2 = null;
            if (reader.Read() && reader.MoveToContent() == XmlNodeType.Element && string.CompareOrdinal(reader.NamespaceURI, strB) == 0 && string.CompareOrdinal(reader.LocalName, text) == 0 && reader.Depth == 3 && PackagingUtilities.GetNonXmlnsAttributeCount(reader) == num)
            {
                while (reader.Read())
                {
                    if (string.CompareOrdinal(reader.NamespaceURI, strB) == 0 && reader.MoveToContent() == XmlNodeType.Element && reader.Depth == 4)
                    {
                        if (string.CompareOrdinal(reader.LocalName, strB2) == 0 && PackagingUtilities.GetNonXmlnsAttributeCount(reader) == num)
                        {
                            if (text2 == null && reader.Read() && reader.MoveToContent() == XmlNodeType.Text && reader.Depth == 5)
                            {
                                text2 = reader.ReadContentAsString();
                                continue;
                            }

                            throw new XmlException(SR.Get("PackageSignatureCorruption"));
                        }

                        if (string.CompareOrdinal(reader.LocalName, strB3) == 0 && PackagingUtilities.GetNonXmlnsAttributeCount(reader) == num)
                        {
                            if (timeFormat == null && reader.Read() && reader.MoveToContent() == XmlNodeType.Text && reader.Depth == 5)
                            {
                                timeFormat = reader.ReadContentAsString();
                                continue;
                            }

                            throw new XmlException(SR.Get("PackageSignatureCorruption"));
                        }

                        throw new XmlException(SR.Get("PackageSignatureCorruption"));
                    }

                    if (string.CompareOrdinal(text, reader.LocalName) == 0 && reader.NodeType == XmlNodeType.EndElement)
                    {
                        if (reader.Read() && reader.MoveToContent() == XmlNodeType.EndElement && string.CompareOrdinal(strA, reader.LocalName) == 0)
                        {
                            break;
                        }

                        throw new XmlException(SR.Get("PackageSignatureCorruption"));
                    }

                    throw new XmlException(SR.Get("PackageSignatureCorruption"));
                }

                if (text2 != null && timeFormat != null)
                {
                    return XmlFormattedTimeToDateTime(text2, timeFormat);
                }

                throw new XmlException(SR.Get("PackageSignatureCorruption"));
            }

            throw new XmlException(SR.Get("RequiredTagNotFound", text));
        }

        private static string DateTimeToXmlFormattedTime(DateTime dt, string format)
        {
            DateTimeFormatInfo dateTimeFormatInfo = new DateTimeFormatInfo();
            dateTimeFormatInfo.FullDateTimePattern = format;
            return dt.ToString(format, dateTimeFormatInfo);
        }

        private static DateTime XmlFormattedTimeToDateTime(string s, string format)
        {
            string[] formats = ConvertXmlFormatStringToDateTimeFormatString(format);
            DateTimeFormatInfo dateTimeFormatInfo = new DateTimeFormatInfo();
            dateTimeFormatInfo.FullDateTimePattern = format;
            return DateTime.ParseExact(s, formats, dateTimeFormatInfo, DateTimeStyles.AllowLeadingWhite | DateTimeStyles.AllowTrailingWhite | DateTimeStyles.NoCurrentDateDefault);
        }

        private static int GetIndex(string format)
        {
            for (int i = 0; i < _dateTimePatternMap.GetLength(0); i++)
            {
                if (string.CompareOrdinal(_dateTimePatternMap[i].Format, format) == 0)
                {
                    return i;
                }
            }

            return -1;
        }

        private static string[] ConvertXmlFormatStringToDateTimeFormatString(string format)
        {
            return _dateTimePatternMap[GetIndex(format)].Patterns;
        }

        private static bool VerifyIdAttribute(XmlReader reader)
        {
            string attribute = reader.GetAttribute(XTable.Get(XTable.ID.SignaturePropertyIdAttrName));
            if (attribute != null && string.CompareOrdinal(attribute, XTable.Get(XTable.ID.SignaturePropertyIdAttrValue)) == 0)
            {
                return true;
            }

            return false;
        }

        private static bool VerifyTargetAttribute(XmlReader reader, string signatureId)
        {
            string attribute = reader.GetAttribute(XTable.Get(XTable.ID.TargetAttrName));
            if (attribute != null)
            {
                if (string.CompareOrdinal(attribute, string.Empty) == 0)
                {
                    return true;
                }

                if (signatureId != null && string.CompareOrdinal(attribute, "#" + signatureId) == 0)
                {
                    return true;
                }

                return false;
            }

            return false;
        }
    }
}
