using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;

namespace EBH_RemoteSigning_Service_ERS_.clsUtilities
{
    public class GlobalVar
    {
		private string  _secretKey;
		public string  SecretKey
		{
			get 
			{
				if (string.IsNullOrEmpty(_secretKey))
				{
					_secretKey = ConfigurationManager.AppSettings["SECRETKEY"].ToString();
				}
				return _secretKey; 
			}
		}
	}

	public class ConfigRequest
	{
		public string sp_id;
		public string sp_password;
		public string uid;
		public string serial_number;

        public ConfigRequest()
        {
			InitValue();
        }

		private void InitValue()
		{
			sp_id = ConfigurationManager.AppSettings["SP_ID"].ToString();
			sp_password = ConfigurationManager.AppSettings["SP_ID"].ToString();
            uid = ConfigurationManager.AppSettings["SP_ID"].ToString();
            serial_number = ConfigurationManager.AppSettings["SP_ID"]?.ToString();
        }
    }
}