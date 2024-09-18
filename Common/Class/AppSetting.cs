using System;
using System.Configuration;
using System.IO;
using System.Runtime.CompilerServices;
using System.Web;
using System.Windows.Forms;

namespace Common.Class
{
    public class AppSetting
    {
        public string ApiURL
        {
            get
            {
                if (ConfigurationManager.AppSettings["ApiURL"] != null)
                {
                    return ConfigurationManager.AppSettings["ApiURL"].ToString();
                }
                else
                {
                    return String.Empty;
                }
            }
        }

        public string AppPath
        {
            get
            {
                if (ConfigurationManager.AppSettings["AppPath"] != null)
                {
                    return ConfigurationManager.AppSettings["AppPath"].ToString();
                }
                else
                {
                    return Application.StartupPath;
                }
            }
        }

        public String ServerAppPath
        {
            get
            {
                if (ConfigurationManager.AppSettings["ServerAppPath"] != null)
                {
                    return ConfigurationManager.AppSettings["ServerAppPath"].ToString();
                }
                else
                {
                    return HttpContext.Current.Server.MapPath("~");
                }
            }
        }

        public string ResourceServerPath
        {
            get
            {
                if (ConfigurationManager.AppSettings["ResourceServerPath"] != null)
                {
                    return ConfigurationManager.AppSettings["ResourceServerPath"].ToString();
                }
                else
                {
                    return Application.StartupPath;
                }
            }
        }

        public string ServerZipPath
        {
            get
            {
                if (ConfigurationManager.AppSettings["ServerZipPath"] != null)
                {
                    return ConfigurationManager.AppSettings["ServerZipPath"].ToString();
                }
                else
                {
                    string zipPath = Path.Combine(ServerAppPath, "FileRSZip");
                    if (!Directory.Exists(zipPath))
                    {
                        Directory.CreateDirectory(zipPath); 
                    }
                    return zipPath;
                }
            }
        }

        private string _LogDir;
        public string LogDir
        {
            get
            {
                if (string.IsNullOrEmpty(_LogDir))
                {
                    _LogDir = Utilities.Common.PathCombine(AppPath, "Logs");
                }

                if (Directory.Exists(_LogDir) == false)
                {
                    Directory.CreateDirectory(_LogDir);
                }
                return _LogDir;
            }
        }

        private string _ServerLogDir;
        public string ServerLogDir
        {
            get
            {
                if (string.IsNullOrEmpty(_ServerLogDir))
                {
                    _LogDir = Utilities.Common.PathCombine(ServerAppPath, "Logs");
                }

                if (Directory.Exists(_ServerLogDir) == false)
                {
                    Directory.CreateDirectory(_ServerLogDir);
                }
                return _ServerLogDir;
            }
        }

        private string _DBConnStr = string.Empty;
        public string DBConnStr
        {
            get
            {
                if (string.IsNullOrEmpty(_DBConnStr))
                {
                    _DBConnStr = ConfigurationManager.ConnectionStrings["DBConnStr"].ConnectionString;
                }
                return _DBConnStr;
            }
        }

        private string _DBConnStrConFig = string.Empty;
        public string DBConnStrConFig
        {
            get
            {
                if (string.IsNullOrEmpty(_DBConnStrConFig))
                {
                    _DBConnStrConFig = ConfigurationManager.ConnectionStrings["DBConnStrConFig"].ConnectionString;
                }
                return _DBConnStrConFig;
            }
        }



        private string _userNameIVAN = string.Empty;
        public string UserNameIVAN
        {
            get
            {
                if (string.IsNullOrEmpty(_userNameIVAN))
                {
                    _userNameIVAN = ConfigurationManager.AppSettings["UserNameIVAN"].ToString();
                }
                return _userNameIVAN;
            }
        }


        public string _passwordIVAN = string.Empty;
        public string PassWordIVAN
        {
            get
            {
                if (string.IsNullOrEmpty(_passwordIVAN))
                {
                    _passwordIVAN = ConfigurationManager.AppSettings["PassWordIVAN"].ToString();
                }
                return _passwordIVAN; ;
            }
        }

        private string _encryptionKey = string.Empty;
        public string EncryptionKey
        {
            get
            {
                if (string.IsNullOrEmpty(_encryptionKey))
                {
                    _encryptionKey = ConfigurationManager.AppSettings["Encrypt"].ToString();
                }
                return _encryptionKey;
            }
        }


        public string AppSendCode
        {
            get
            {
                return ConfigurationManager.AppSettings["AppSendCode"].ToString();
            }
        }

        public string AppSecretCode
        {
            get
            {
                return ConfigurationManager.AppSettings["AppSecretCode"].ToString();
            }
        }

        public string ValidXMLPath
        {
            get
            {
                //return ConfigurationManager.AppSettings["ValidXMLPath"].ToString();
                return "E:\\Shared\\IVANGenXMLApi\\bin\\ValidXML"; // Cái này đưa vào cấu hình
            }
        }

        public string DBDanhMuc
        {
            get
            {
                return ConfigurationManager.AppSettings["DBDanhMuc"].ToString();
            }
        }

        public string XMLToSendVersion { get { return ConfigurationManager.AppSettings["XMLToSendVersion"].ToString(); } }
        public string XMLToSendUpdate { get { return ConfigurationManager.AppSettings["XMLToSendUpdate"].ToString(); } }

    }
}
