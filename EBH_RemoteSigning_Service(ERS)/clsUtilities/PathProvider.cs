using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Web;

namespace EBH_RemoteSigning_Service_ERS.clsUtilities
{
    public class PathProvider
    {
		private string _appPath;
		public string AppPath
		{
			get 
			{
				if (string.IsNullOrEmpty(_appPath))
				{
					_appPath = HttpContext.Current.Server.MapPath("~"); ;
				}
				return _appPath; 
			}
		}

		private string _logPath;
		public string LogPath
		{
			get 
			{
				if (string.IsNullOrEmpty(_logPath))
				{
					string logFolder = ConfigurationManager.AppSettings["LOGFOLDER"].ToString();
					_logPath = Path.Combine(AppPath,logFolder);
				}
				return _logPath; 
			}
		}
	}
}