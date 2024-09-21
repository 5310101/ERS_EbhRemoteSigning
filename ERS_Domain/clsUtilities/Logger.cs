using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;

namespace ERS_Domain.clsUtilities
{
    public class Logger
    {
        private string EnvBreak = "-----------------";

        private void CheckFilePath(string filePath)
        {
            if (!Directory.Exists(Utilities.globalPath.LogPath))
            {
                Directory.CreateDirectory(Utilities.globalPath.LogPath);
            }
            if (!File.Exists(filePath))
            {
                using (FileStream fs = new FileStream(filePath, FileMode.Create)) { }
            }
        }

        public void ErrorLog(Exception ex , string title, params string[] moreInfos)
        {
			try
			{
                string fileName = $"ERS_errlog_{DateTime.Now:ddMMyyyy}.txt";
                string filePath = Path.Combine(Utilities.globalPath.LogPath, fileName);
                CheckFilePath(filePath);
                StringBuilder sb = new StringBuilder(); 
                sb.AppendLine($"Title: {title}");
                sb.AppendLine($"Time: {DateTime.Now:dd/MM/yyyy : HH:mm;ss}");
                sb.AppendLine($"Message: {ex.Message}");
                sb.AppendLine($"StackTrace: {ex.StackTrace}");
                if(ex.InnerException != null)
                {
                    sb.AppendLine($"InnerException: {ex.InnerException}");
                }
                if(moreInfos != null)
                {
                    sb.AppendLine("More info: ");
                    foreach(string info in moreInfos)
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

        public void ErrorLog(string exMessage, string title, params string[] moreInfos)
        {
            try
            {
                string fileName = $"ERS_errlog_{DateTime.Now:ddMMyyyy}.txt";
                string filePath = Path.Combine(Utilities.globalPath.LogPath, fileName);
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

        public void InfoLog(string title,string Info ,params string[] moreInfos)
        {
            try
            {
                string fileName = $"ERS_inflog_{DateTime.Now:ddMMyyyy}.txt";
                string filePath = Path.Combine(Utilities.globalPath.LogPath, fileName);
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