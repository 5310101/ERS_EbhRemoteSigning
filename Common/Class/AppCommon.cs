using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Common.Class
{
    public class AppCommon
    {
        public string ObjectToXML<T>(T Source)
        {
            string result = string.Empty;

            using (StringWriter writer = new StringWriter())
            {
                System.Xml.Serialization.XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer(typeof(T));
                serializer.Serialize(writer, Source);
                result = writer.ToString();
            }
            return result;
        }

        public string PathCombine(params string[] Paths)
        {
            if (Paths.Length <= 0) { return string.Empty; }
            string firstPath = Paths[0];

            string filePath = string.Empty;
            bool isFirstPath = true;
            foreach (string nextPath in Paths)
            {
                if (isFirstPath) { isFirstPath = false; continue; }
                filePath = Path.Combine(filePath, nextPath);
            }

            return firstPath.TrimEnd(new char[] { '\\' }) + @"\" + filePath.TrimStart(new char[] { '\\' });
        }


        public string Encrypt(string clearText, string EnKey = "")
        {
            if (string.IsNullOrEmpty(EnKey)) { EnKey = Utilities.Setting.EncryptionKey; }
            byte[] clearBytes = Encoding.UTF8.GetBytes(clearText);
            using (Aes encryptor = Aes.Create())
            {
                Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(EnKey, new byte[] { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 });
                encryptor.Key = pdb.GetBytes(32);
                encryptor.IV = pdb.GetBytes(16);
                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, encryptor.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(clearBytes, 0, clearBytes.Length);
                        cs.Close();
                    }
                    clearText = Convert.ToBase64String(ms.ToArray());
                }
            }
            return clearText;
        }
        public string Decrypt(string cipherText, string EnKey = "")
        {
            if (string.IsNullOrEmpty(EnKey)) { EnKey = Utilities.Setting.EncryptionKey; }
            cipherText = cipherText.Replace(" ", "+");
            byte[] cipherBytes = Convert.FromBase64String(cipherText);
            using (Aes encryptor = Aes.Create())
            {
                Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(EnKey, new byte[] { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 });
                encryptor.Key = pdb.GetBytes(32);
                encryptor.IV = pdb.GetBytes(16);
                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, encryptor.CreateDecryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(cipherBytes, 0, cipherBytes.Length);
                        cs.Close();
                    }
                    cipherText = Encoding.UTF8.GetString(ms.ToArray());
                }
            }
            return cipherText;
        }

        public string GetExcelColumnName(int columnNumber)
        {
            string columnName = "";

            while (columnNumber > 0)
            {
                int modulo = (columnNumber - 1) % 26;
                columnName = Convert.ToChar('A' + modulo) + columnName;
                columnNumber = (columnNumber - modulo) / 26;
            }

            return columnName;
        }


        public string MD5Hash(string input)
        {
            StringBuilder hash = new StringBuilder();
            MD5CryptoServiceProvider md5provider = new MD5CryptoServiceProvider();
            byte[] bytes = md5provider.ComputeHash(new UTF8Encoding().GetBytes(input));

            for (int i = 0; i < bytes.Length; i++)
            {
                hash.Append(bytes[i].ToString("x2"));
            }
            return hash.ToString();
        }


        private string GetRelativePath(string FilePath)
        {
            try
            {
                string[] arr = FilePath.Split('\\');

                StringBuilder sb = new StringBuilder();
                int cnt = 0;
                foreach (string item in arr)
                {
                    if (string.IsNullOrWhiteSpace(item)) { continue; }
                    if (cnt <= 1) { cnt++; continue; }
                    sb.Append(@"\" + item);
                }
                return sb.ToString();
            }
            catch (Exception ex)
            {
                Utilities.Log.GhiLog("GetRelativePath", ex);
                return string.Empty;
            }
        }
    }
}
