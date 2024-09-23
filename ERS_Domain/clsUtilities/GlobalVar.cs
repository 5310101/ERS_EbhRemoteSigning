using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;

namespace ERS_Domain.clsUtilities
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
					_secretKey = ConfigurationManager.AppSettings["SECRETKEY"];
				}
				return _secretKey; 
			}
		}

		private ConfigRequest configRequest;
		public ConfigRequest ConfigRequest
		{
			get
			{
				if(configRequest == null)
				{
					configRequest = new ConfigRequest();	
				}
				return configRequest;
			}
		}
	}

	public class ConfigRequest
	{
		public string sp_id;
		public string sp_password;

        public ConfigRequest()
        {
			InitValue();
        }

		private void InitValue()
		{
			sp_id = ConfigurationManager.AppSettings["SP_ID"];
			sp_password = ConfigurationManager.AppSettings["SP_PASSWORD"];
        }
    }

	public static class VNPT_URI
	{
        public static string uriGetCert = "https://gwsca.vnpt.vn/sca/sp769/v1/credentials/get_certificate";
        public static string uriSign = "https://gwsca.vnptit.vn/sca/sp769/v1/signatures/sign";
        
        public static string uriGetCert_test = "https://rmgateway.vnptit.vn/sca/sp769/v1/credentials/get_certificate";
        public static string uriSign_test = "https://rmgateway.vnptit.vn/sca/sp769/v1/signatures/sign";
    }
}