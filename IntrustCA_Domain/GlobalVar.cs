using System;
using System.Configuration;
using System.IO;
using System.Web;

namespace IntrustCA_Domain
{
    public static class GlobalVar
    {
        public static string AppPath = System.Web.HttpContext.Current != null ? HttpContext.Current.Server.MapPath("~/") : AppDomain.CurrentDomain.BaseDirectory;
        public static string LogPath = Path.Combine(AppPath, ConfigurationManager.AppSettings["LOGFOLDER"]);
    }
}
