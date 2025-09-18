using IntrustCA_Domain;
using IntrustCA_Domain.Dtos;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntrustCA_Domain
{
    public static class Utils
    {
        public static byte[] Base64ToData(this string base64String)
        {
            if (string.IsNullOrEmpty(base64String))
            {
                return null;
            }
            return Convert.FromBase64String(base64String);
        }
        public static string DataToBase64(this byte[] data)
        {
            if (data == null || data.Length == 0)
            {
                return null;
            }
            return Convert.ToBase64String(data);
        }

        public static string Base64ToText(this string base64Data)
        {
            if (string.IsNullOrEmpty(base64Data))
            {
                return "";
            }
            var bytes = Convert.FromBase64String(base64Data);
            return System.Text.Encoding.UTF8.GetString(bytes);
        }

        public static string TextToBase64(this string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return "";
            }
            var bytes = System.Text.Encoding.UTF8.GetBytes(text);
            return Convert.ToBase64String(bytes);

        }
    }
}


