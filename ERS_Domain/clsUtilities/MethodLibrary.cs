using ERS_Domain.Model;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.CodeDom;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;

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
                return Path.GetExtension(tenFile).Replace("-595", "");
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
                Utilities.logger.ErrorLog("Service return null response","Server SmartCA Error");
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
        public static string ExportSigner(SignerInfo signer, string pathTempHS, string transaction_id)
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
        public static SignerInfo ImportSigner(string filePath)
        {
            string json = File.ReadAllText(filePath);
            SignerInfo output = JsonConvert.DeserializeObject<SignerInfo>(json);
            return output;
        }
    }
}