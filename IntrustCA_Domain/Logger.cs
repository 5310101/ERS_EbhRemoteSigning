using iTextSharp.text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntrustCA_Domain
{
    public static class Logger
    {
        private static string EnvBreak = "-----------------";

        private static void CheckFilePath(string filePath)
        {
            if (!Directory.Exists(GlobalVar.LogPath))
            {
                Directory.CreateDirectory(GlobalVar.LogPath);
            }
            if (!File.Exists(filePath))
            {
                using (FileStream fs = new FileStream(filePath, FileMode.Create)) { }
            }
        }

        public static void ErrorLog(Exception ex, string title, params string[] moreInfos)
        {
            try
            {
                string fileName = $"ERS_errlog_{DateTime.Now:ddMMyyyy}.txt";
                string filePath = Path.Combine(GlobalVar.LogPath, fileName);
                CheckFilePath(filePath);
                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"Title: {title}");
                sb.AppendLine($"Time: {DateTime.Now:dd/MM/yyyy : HH:mm:ss}");
                sb.AppendLine($"Message: {ex.Message}");
                sb.AppendLine($"StackTrace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    sb.AppendLine($"InnerException: {ex.InnerException}");
                }
                if (moreInfos != null)
                {
                    sb.AppendLine("More info: ");
                    foreach (string info in moreInfos)
                    {
                        sb.AppendLine(info);
                    }
                }
                sb.AppendLine(EnvBreak);
                File.AppendAllText(filePath, sb.ToString());
            }
            catch
            {
            }
        }

        public static void ErrorLog(string exMessage, string title, params string[] moreInfos)
        {
            try
            {
                string fileName = $"ERS_errlog_{DateTime.Now:ddMMyyyy}.txt";
                string filePath = Path.Combine(GlobalVar.LogPath, fileName);
                CheckFilePath(filePath);
                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"Title: {title}");
                sb.AppendLine($"Time: {DateTime.Now:dd/MM/yyyy : HH:mm:ss}");
                sb.AppendLine($"Message: {exMessage}");
                if (moreInfos != null)
                {
                    sb.AppendLine("More info: ");
                    foreach (string info in moreInfos)
                    {
                        sb.AppendLine(info);
                    }
                }
                sb.AppendLine(EnvBreak);
                File.AppendAllText(filePath, sb.ToString());
            }
            catch
            {
            }
        }

        public static void InfoLog(string title, string Info, params string[] moreInfos)
        {
            try
            {
                string fileName = $"ERS_inflog_{DateTime.Now:ddMMyyyy}.txt";
                string filePath = Path.Combine(GlobalVar.LogPath, fileName);
                CheckFilePath(filePath);
                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"Title: {title}");
                sb.AppendLine($"Time: {DateTime.Now:dd/MM/yyyy : HH:mm:ss}");
                sb.AppendLine($"Info: {Info}");
                if (moreInfos != null)
                {
                    sb.AppendLine("More info: ");
                    foreach (string info in moreInfos)
                    {
                        sb.AppendLine(info);
                    }
                }
                sb.AppendLine(EnvBreak);
                File.AppendAllText(filePath, sb.ToString());
            }
            catch
            {
            }
        }
    }
}
