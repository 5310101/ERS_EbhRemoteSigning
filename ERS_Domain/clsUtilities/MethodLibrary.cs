using ERS_Domain.CustomSigner;
using Newtonsoft.Json;
using RestSharp;
using RestSharp.Extensions;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Web.SessionState;
using System.Xml;
using System.Xml.Serialization;
using VnptHashSignatures.Interface;

namespace ERS_Domain.clsUtilities
{
    public static class MethodLibrary
    {
        public static string MD5Hasher(string intput)
        {
            using (MD5 md5 = MD5.Create())
            {
                byte[] stringByte = Encoding.ASCII.GetBytes(intput);
                byte[] hashByte = md5.ComputeHash(stringByte);
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashByte.Length; i++)
                {
                    sb.Append(hashByte[i].ToString("X2"));
                }
                return sb.ToString();
            }
        }

        public static string SafeString(this object input)
        {
            if (input == null || input is DBNull)
            {
                return string.Empty;
            }
            return input.ToString();
        }
        public static T SafeNumber<T>(this object input) where T : struct, IConvertible
        {
            if (input == null || input is DBNull)
            {
                return default;
            }
            return (T)Convert.ChangeType(input, typeof(T));
        }

        public static DateTime SafeDateTime(this object input, string DateType = "MM/dd/yyyy")
        {
            try
            {
                if (input == null || input is DBNull)
                {
                    return default;
                }
                if (DateType == "MM/dd/yyyy") return DateTime.Parse(input.ToString());
                return DateTime.ParseExact(input.ToString(), DateType, DateTimeFormatInfo.InvariantInfo);
            }
            catch (Exception)
            {
                return default;
            }
        }

        public static string GetMaTK(string tenFile)
        {
            string[] extensions = { ".pdf", ".docx", ".xlsx" };
            if (extensions.Contains(Path.GetExtension(tenFile)))
            {
                return "CT-DK";
            }
            else
            {
                return Path.GetFileNameWithoutExtension(tenFile).Replace("-595", "");
            }
        }

        public static String Query(object req, string serviceUri)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            //ServicePointManager.ServerCertificateValidationCallback
            //    += new RemoteCertificateValidationCallback(SslHelper.ValidateRemoteCertificate);

            RestClient client = new RestClient(serviceUri);
            RestRequest request = new RestRequest(Method.POST);
            request.AddHeader("Content-Type", "application/json");
            var body = JsonConvert.SerializeObject(req);
            request.AddParameter("application/json", body, ParameterType.RequestBody);
            IRestResponse response = null;
            try
            {
                response = client.Execute(request);
            }
            catch (Exception ex)
            {
                Utilities.logger.ErrorLog($"Connect gateway error: {ex.Message}", "Server SmartCA Error");
                return null;
            }

            if (response == null || response.ErrorException != null)
            {
                Utilities.logger.ErrorLog("Service return null response", "Server SmartCA Error");
                return null;
            }
            if (response.StatusCode != HttpStatusCode.OK)
            {
                Utilities.logger.ErrorLog($"Status code={response.StatusCode}. Status content: {response.Content}", "Server SmartCA Error");
                return null;
            }

            return response.Content;
        }

        //luu tru thong tin signer de sau khi lay ket qua tao signer moi
        public static string ExportSigner(SignerProfile signer, string pathTempHS, string transaction_id)
        {
            try
            {
                string json = JsonConvert.SerializeObject(signer);
                if (string.IsNullOrEmpty(json))
                {
                    return "";
                }
                string pathSigner = Path.Combine(pathTempHS, $"{transaction_id}.json");
                File.WriteAllText(pathSigner, json);
                return pathSigner;
            }
            catch (Exception ex)
            {
                Utilities.logger.ErrorLog(ex, "ExportSigner");
                return "";
            }
        }
        //import lai thong tin signer da luu tru
        public static SignerProfile ImportSigner(string filePath)
        {
            string json = File.ReadAllText(filePath);
            SignerProfile output = JsonConvert.DeserializeObject<SignerProfile>(json);
            return output;
        }

        public static IHashSigner GenerateCustomSigner(byte[] unsignData, string certBase64)
        {
            if (string.IsNullOrEmpty(certBase64))
            {
                throw new FormatException("Bas64 must not be null");
            }

            try
            {
                Convert.FromBase64String(certBase64);
            }
            catch (FormatException ex)
            {
                throw ex;
            }
            return new CustomXmlSigner(unsignData, certBase64);

        }
        public static bool VerifySignature(byte[] signedDocBytes, string idSignature)
        {
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

                try
                {
                    val.LoadXml((XmlElement)elementsByTagName[0]);
                }
                catch 
                {

                }
                return val.CheckSignature();
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public static string GetSubjectValue(this string subject, string subjectName)
        {
            string[] subjArr = subject.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
            var subjFinded = subjArr.FirstOrDefault(x => x.Contains(subjectName));
            var subjValue = subjFinded.Split(new string[] { "=" }, StringSplitOptions.RemoveEmptyEntries)[1];
            return subjValue == null ? "" : subjValue.Trim();
        }

        public static string FromBase64ToString(this string base64)
        {
            if (string.IsNullOrEmpty(base64))
            {
                return "";
            }
            try
            {
                byte[] bytes = Convert.FromBase64String(base64);
                return Encoding.UTF8.GetString(bytes);
            }
            catch 
            {
                //Utilities.logger.ErrorLog(ex, "FromBase64ToString");
                return "";
            }
        }

        public static bool SerializeToFile<T>(T obj, string filePath)
        {
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(T));
                XmlWriterSettings settings = new XmlWriterSettings()
                {
                    Encoding = new UTF8Encoding(false),
                    Indent = true
                };

                string xmlString = "";
                using (var stream = new MemoryStream())
                using (var writer = XmlWriter.Create(stream, settings))
                {
                    serializer.Serialize(writer, obj);
                    xmlString = Encoding.UTF8.GetString(stream.ToArray());
                }
                File.WriteAllText(filePath, xmlString);
                return true;
            }
            catch 
            {
                //Utilities.logger.ErrorLog(ex, "SerializeToFile");
                return false;
            }
        }

        public static byte[] GetBytesStringFromJsonObject(this object input)
        {
            string json = JsonConvert.SerializeObject(input);
            return Encoding.UTF8.GetBytes(json);
        }

        public static T DeserializeJsonTo<T>(this string jsonData)
        {
            return JsonConvert.DeserializeObject<T>(jsonData);
        }

        public static string GetMaThuTuc(this string tenMaHS)
        {
            return tenMaHS.Replace("-595", "").Replace("-959", "").Replace("_959","").Replace("_595", "").Replace("_166","").Replace("-166","");
        }
    }
}