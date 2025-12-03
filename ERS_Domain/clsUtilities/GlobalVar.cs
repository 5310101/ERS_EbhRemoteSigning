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

    public abstract class ConfigBase
    {
        public string sp_id { get; set; }
        public string sp_password { get; set; }
        protected abstract void InitValue();
        
    }

    public class ConfigRequest : ConfigBase
    {
        public ConfigRequest() : base()
        {
            InitValue();
        }

        protected override void InitValue()
        {
            sp_id = ConfigurationManager.AppSettings["SP_ID"];
            sp_password = ConfigurationManager.AppSettings["SP_PASSWORD"];
        }
    }

    public class CA2ConfigRequest : ConfigBase
    {
        public CA2ConfigRequest()
        {
            InitValue();
        }
        protected override void InitValue()
        {
            sp_id = ConfigurationManager.AppSettings["CA2_SP_ID"];
            sp_password = ConfigurationManager.AppSettings["CA2_SP_PASSWORD"];
        }
    }

    public static class VNPT_URI
    {
        public static readonly string URI =
            ConfigurationManager.AppSettings["ISTEST"] == "1"
                ? ConfigurationManager.AppSettings["URL_ENDPOINT_TEST"]
                : ConfigurationManager.AppSettings["URL_ENDPOINT"];

        public static readonly string uriGetCert = URI + "/v1/credentials/get_certificate";
        public static readonly string uriSign = URI + "/v1/signatures/sign";
        public static readonly string uriGetResult = URI + "/v1/signatures/sign";

    }

    public static class CA2_URI
    {
        public static readonly string URI = ConfigurationManager.AppSettings["CA2API_URI"];

        public static readonly string uriGetCert = URI + "/get_certificate";
        public static readonly string uriSign = URI + "/sign";
        public static readonly string uriGetResult = URI + "/status";
    }

}