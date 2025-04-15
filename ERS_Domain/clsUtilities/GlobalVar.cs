using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;

namespace ERS_Domain.clsUtilities
{
    public class GlobalVar
    {
        private string _secretKey;
        public string SecretKey
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
                if (configRequest == null)
                {
                    configRequest = new ConfigRequest();
                }
                return configRequest;
            }
        }
    }

    public class ConfigRequest
    {
        public string sp_id { get; set; }
        public string sp_password { get; set; }

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
        public static readonly string URI =
            ConfigurationManager.AppSettings["ISTEST"] == "1"
                ? ConfigurationManager.AppSettings["URL_ENDPOINT"]
                : ConfigurationManager.AppSettings["URL_ENDPOINT_TEST"];

        public static readonly string uriGetCert = URI + "/v1/credentials/get_certificate";
        public static readonly string uriSign = URI + "/v1/signatures/sign";
        public static readonly string uriGetResult = URI + "/v1/signatures/sign";

    }
}