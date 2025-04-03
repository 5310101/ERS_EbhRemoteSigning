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
		public static string URI = ConfigurationManager.AppSettings["URL_ENDPOINT"];
        public static string URI_TEST = ConfigurationManager.AppSettings["URL_ENDPOINT_TEST"];

        public static string uriGetCert = $"{URI}/v1/credentials/get_certificate";
        public static string uriSign = $"{URI}/v1/signatures/sign";
		public static string uriGetResult = $"{URI}/v1/signatures/sign";

        public static string uriGetCert_test = $"{URI_TEST}/v1/credentials/get_certificate";
        public static string uriSign_test = $"{URI_TEST}/v1/signatures/sign";
        public static string uriGetResult_test = $"{URI_TEST}/v1/signatures/sign";
    }
}