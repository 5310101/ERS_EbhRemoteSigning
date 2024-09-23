using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Web;

namespace ERS_Domain.clsUtilities
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
					if(System.Web.HttpContext.Current != null)
					{
                        _appPath = HttpContext.Current.Server.MapPath("~"); ;
                    }
					else
					{
						_appPath = AppDomain.CurrentDomain.BaseDirectory;
					}
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
					string logFolder = ConfigurationManager.AppSettings["LOGFOLDER"];
					_logPath = Path.Combine(AppPath,logFolder);
				}
				return _logPath; 
			}
		}

        private string _signedTempFolder;
        public string SignedTempFolder
        {
            get
            {
                if (string.IsNullOrEmpty(_signedTempFolder))
                {
                    string TempFolderName = ConfigurationManager.AppSettings["SIGNEDTEMPFOLDER"];
                    _signedTempFolder = Path.Combine(AppPath, TempFolderName);
                }
                return _signedTempFolder;
            }
        }
    }
}