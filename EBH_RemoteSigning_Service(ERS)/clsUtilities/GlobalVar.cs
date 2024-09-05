using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;

namespace EBH_RemoteSigning_Service_ERS.clsUtilities
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

	public static class VNPT_URI
	{
        public static string uriGetCert = @"https://gwsca.vnpt.vn/sca/sp769/v1/credentials/get_certificate";
        public static string uriSign = @"https://gwsca.vnpt.vn/sca/sp769/v1/credentials/get_certificate";
        public static string uriGetStatus = @"https://gwsca.vnpt.vn/sca/sp769/v1/credentials/get_certificate";

        public static string uriGetCert_test = @"https://rmgateway.vnptit.vn/sca/sp769/v1/credentials/get_certificate";
        public static string uriSign_test = @"https://rmgateway.vnptit.vn/sca/sp769/v1/credentials/get_certificate";
        public static string uriGetStatus_test = @"https://rmgateway.vnptit.vn/sca/sp769/v1/credentials/get_certificate";
    }
}