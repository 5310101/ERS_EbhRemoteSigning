using Newtonsoft.Json;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using System.Xml.Serialization;



namespace Common
{
    public class UTF8StringWriter : StringWriter { public override Encoding Encoding { get { return Encoding.UTF8; } } }

    public static class Extensions
    {
        public static string EncodeMD5(this string data)
        {
            MD5CryptoServiceProvider provider = new MD5CryptoServiceProvider();
            byte[] dataBytes = ASCIIEncoding.Default.GetBytes(data);
            byte[] hashBytes = provider.ComputeHash(dataBytes);
            return BitConverter.ToString(hashBytes).ToLower().Replace("-", string.Empty);
        }
        public static string ToXML<T>(this T ObjectToSerialize)
        {
            XmlSerializer xmlSerializer = new XmlSerializer(ObjectToSerialize.GetType());
            using (StringWriter textWriter = new UTF8StringWriter())
            {
                xmlSerializer.Serialize(textWriter, ObjectToSerialize);
                return textWriter.ToString().Replace(" xmlns=\"http://tempuri.org/\"", "").Replace("xmlns:xsd=\"http://w3.org/2001/XMLSchema\"", "");
            }
        }

        public static string ToJson<T>(this T ObjectToSerialize)
        {
            return JsonConvert.SerializeObject(ObjectToSerialize);
        }



        public static T ToObject<T>(this string sourcetxt) where T : class
        {
            XmlSerializer ser = new XmlSerializer(typeof(T));
            using (StringReader sr = new StringReader(sourcetxt))
            {
                return (T)ser.Deserialize(sr);
            }
        }

        public static string GetRelativePath(this string childFilePath, string rootFilePath)
        {
            if (string.IsNullOrEmpty(childFilePath)) throw new ArgumentNullException(nameof(childFilePath));
            if (string.IsNullOrEmpty(rootFilePath)) throw new ArgumentNullException(nameof(rootFilePath));
            Uri uriChild = new Uri(Path.GetFullPath(childFilePath));
            Uri uriRoot = new Uri(Path.GetFullPath(rootFilePath));
            Uri relativeUri = uriRoot.MakeRelativeUri(uriChild);
            string relativePath = Uri.UnescapeDataString(relativeUri.ToString());
            return relativePath.Replace('/', Path.DirectorySeparatorChar);
        }

        public static T JsonToObject<T>(this string sourcetxt) where T : class
        {
            Newtonsoft.Json.JsonSerializer jsonSerializer = new Newtonsoft.Json.JsonSerializer();
            using (StringReader sr = new StringReader(sourcetxt))
            {
                return (T)jsonSerializer.Deserialize(sr, typeof(T));
            }
        }

        public static void ValidNode(this XmlDocument xDoc, string Path, ref StringBuilder sbMsg, bool IsValidEmpty = false, int MaxLength = -1, bool IsValidNumber = false, bool IsValidDate = false)
        {
            string Value = xDoc.SelectSingleNode(Path).InnerText;

            // Valid trường để trống
            if (IsValidEmpty)
            {
                if (string.IsNullOrWhiteSpace(Value))
                {
                    sbMsg.AppendLine("Giá trị tại thẻ " + Path + " không được để trống");
                }
            }

            // Valid độ dài trường
            if (MaxLength > 0)
            {
                if (Value.Trim().Length > MaxLength)
                {
                    sbMsg.AppendLine("Giá trị tại thẻ " + Path + " vượt quá " + MaxLength + " ký tự");
                }
            }

            // Valid kiểu số
            if (IsValidNumber)
            {
                long outNum;
                if (long.TryParse(Value, out outNum) == false)
                {
                    sbMsg.AppendLine("Giá trị tại thẻ " + Path + " phải là dữ liệu kiểu số (" + Value + ")");
                }
            }

            // Valid kiểu ngày
            if (IsValidDate)
            {
                DateTime outDate;
                if (DateTime.TryParse(Value, out outDate) == false)
                {
                    sbMsg.AppendLine("Giá trị tại thẻ " + Path + " phải là dữ liệu kiểu ngày (" + Value + ")");
                }
            }
        }

        public static void ValidNodeList(this XmlDocument xDoc, string PathLoop, string NodeName, ref StringBuilder sbMsg, bool IsValidEmpty = false, int MaxLength = -1, bool IsValidNumber = false, bool IsValidDate = false)
        {
            XmlNodeList xmlNodeList = xDoc.SelectNodes(PathLoop);
            if (xmlNodeList.Count > 0)
            {
                int nodeCnt = 1;
                foreach (XmlNode node in xmlNodeList)
                {
                    string Value = node.SelectSingleNode(NodeName).InnerText;
                    // Valid trường để trống
                    if (IsValidEmpty)
                    {
                        if (string.IsNullOrWhiteSpace(Value))
                        {
                            sbMsg.AppendLine("Giá trị tại thẻ " + PathLoop + "[" + nodeCnt + "]." + NodeName + " không được để trống");
                        }
                    }

                    // Valid độ dài trường
                    if (MaxLength > 0)
                    {
                        if (Value.Trim().Length > MaxLength)
                        {
                            sbMsg.AppendLine("Giá trị tại thẻ " + PathLoop + "[" + nodeCnt + "]." + NodeName + " vượt quá " + MaxLength + " ký tự");
                        }
                    }

                    // Valid kiểu số
                    if (IsValidNumber)
                    {
                        long outNum;
                        if (long.TryParse(Value, out outNum) == false)
                        {
                            sbMsg.AppendLine("Giá trị tại thẻ " + PathLoop + "[" + nodeCnt + "]." + NodeName + " phải là dữ liệu kiểu số (" + Value + ")");
                        }
                    }

                    // Valid kiểu ngày
                    if (IsValidDate)
                    {
                        DateTime outDate;
                        if (DateTime.TryParse(Value, out outDate) == false)
                        {
                            sbMsg.AppendLine("Giá trị tại thẻ " + PathLoop + "[" + nodeCnt + "]." + NodeName + " phải là dữ liệu kiểu ngày (" + Value + ")");
                        }
                    }

                    nodeCnt++;
                }
            }

        }

        public static string GetHeader(this HttpRequestMessage httpRequestMessage, string headerName)
        {
            try
            {
                return httpRequestMessage.Headers.GetValues(headerName).First();
            }
            catch (Exception)
            {

                return string.Empty;
            }

        }

        public static string ToYearPeriod(this DateTime datetime)
        {
            string time = datetime.ToString("MM");
            string period = string.Empty;
            if (time == "1" || time == "2" || time == "3")
            {
                return period = "01";
            }
            else if (time == "4" || time == "5" || time == "6")
            {
                return period = "02";
            }
            else if (time == "7" || time == "8" || time == "9")
            {
                return period = "03";
            }
            else if (time == "10" || time == "11" || time == "12")
            {
                return period = "04";
            }
            else return period;
        }

        public static string base64StringEnCode(this string stringInput)
        {
            byte[] stringData = Encoding.UTF8.GetBytes(stringInput);
            string base64StringOutput = Convert.ToBase64String(stringData);
            return base64StringOutput;
        }

        public static string Base64StringDecode(this string base64StringInput)
        {
            Utilities.Log.GhiLog("Base64StringDecode", base64StringInput);
            byte[] base64EncodedBytes = Convert.FromBase64String(base64StringInput);
            File.WriteAllBytes(Guid.NewGuid().ToString() + ".xml", base64EncodedBytes);
            string stringOutput = Encoding.UTF8.GetString(base64EncodedBytes);
            return stringOutput;
        }


        //public static DBNull SetNull(this object obj)
        //{
        //        return DBNull.Value;
        //}

        //public static SqlParameter SetNull(this SqlParameter sqlParam)
        //{
        //    if(sqlParam.Value == null) 
        //    {
        //        sqlParam.Value = DBNull.Value;
        //        return sqlParam;
        //    }
        //    return sqlParam;
        //}

        public static object ToSqlValue(this object Input)
        {
            if (Input == DBNull.Value) { return DBNull.Value; }
            return Input;
        }

        public static string GetText(this XmlNode xmlNode, string Path = "")
        {
            if (xmlNode == null) { return string.Empty; }

            if (string.IsNullOrWhiteSpace(Path))
            {
                return xmlNode.InnerText;
            }
            else
            {
                XmlNode nodeCon = xmlNode.SelectSingleNode(Path);
                if (nodeCon == null) { return string.Empty; }
                return nodeCon.InnerText;
            }
        }

        public static string GetText(this XmlDocument xDoc, string Path)
        {
            XmlNode xmlNode = xDoc.SelectSingleNode(Path);
            return xmlNode.GetText();
        }


        public static object StringToSqlData(this string input)
        {
            if (input == string.Empty) { return DBNull.Value; }
            return input;
        }

        public static object GetSQLText(this XmlNode xmlNode, string Path = "")
        {
            if (xmlNode == null) { return DBNull.Value; }

            if (string.IsNullOrWhiteSpace(Path))
            {
                return string.IsNullOrWhiteSpace(xmlNode.InnerText) ? (object)DBNull.Value : xmlNode.InnerText;
            }
            else
            {
                XmlNode nodeCon = xmlNode.SelectSingleNode(Path);
                if (nodeCon == null) { return DBNull.Value; }
                return string.IsNullOrWhiteSpace(nodeCon.InnerText) ? (object)DBNull.Value : nodeCon.InnerText;
            }
        }

        public static object GetSQLText(this XmlDocument xDoc, string Path)
        {
            XmlNode xmlNode = xDoc.SelectSingleNode(Path);
            return xmlNode.GetSQLText();
        }

        public static object CheckToDate(this object input)
        {
            if (input == null || input == DBNull.Value) { return DBNull.Value; }
            DateTime time;
            if (DateTime.TryParse(input.ToString(), out time) == true)
            {

                string stringTime = time.ToString("yyyy/MM/dd");
                return stringTime;
            }
            return time;

        }

        public static object ToDateTimeFormat(this string input, string Format = "dd/MM/yyyy")
        {
            string time = DateTime.ParseExact(input, "dd/MM/yyyy", CultureInfo.InvariantCulture).ToString(Format);
            return time;
        }

        public static SqlParameter GetSqlParameter(this string input, string kieuDuLieu, string Name)
        {
            if (string.IsNullOrEmpty(input)) { return new SqlParameter(Name, DBNull.Value); }

            if (kieuDuLieu == "string")
            {
                return new SqlParameter(Name, SqlDbType.NVarChar) { Value = input.ToString() };
            }
            else if (kieuDuLieu == "DateTime")
            {
                return new SqlParameter(Name, SqlDbType.DateTime) { Value = input.ToDateTimeFormat("yyyy/MM/dd") };
            }
            else if (kieuDuLieu == "DateTimeWH")
            {
                return new SqlParameter(Name, SqlDbType.DateTime) { Value = input.ToDateTimeFormat("yyyy/MM/dd hh:mm:ss") };
            }
            else if (kieuDuLieu == "DateTime_year")
            {
                return new SqlParameter(Name, SqlDbType.DateTime) { Value = DateTime.Parse(input).ToString("yyy") };
            }
            else if (kieuDuLieu == "decimal")
            {
                return new SqlParameter(Name, SqlDbType.Decimal) { Value = decimal.Parse(input) };
            }
            else if (kieuDuLieu == "int")
            {
                return new SqlParameter(Name, SqlDbType.Int) { Value = int.Parse(input) };
            }
            return null;
        }
    }
}
