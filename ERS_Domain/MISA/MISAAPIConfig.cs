using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERS_Domain.MISA
{
    public class MISAAPIConfig
    {
        public string clientId { get; set; }
        public string clientKey { get; set; }
    }

    public static class MISAAPIUrl
    {
        public const string baseUrl = "https://esignapp.misa.vn/";
        public const string signIn = baseUrl + "api/auth/api/v1/auth/login-api";
        public const string refreshToken = baseUrl + "webdev/api/auth/api/v1/auth/refreshtoken";
        public const string twoFactor = baseUrl + "api/auth/api/v1/auth/two-factor-auth";
        public const string resendOTP = baseUrl + "webdev/api/auth/api/v1/auth/resend-otp-auth";
        public const string getCerts = baseUrl + "external/esrm/service/general/api/v1/Certificates/by-userId";
        public const string getCert = baseUrl + "external/esrm/service/general/api/v1/Certificates/by-certId";
        public const string hashFile = baseUrl + "external/esrm/service/document/api/v1/documents/hash";
        public const string signHash = baseUrl + "external/esrm/service/signing/api/v1/Signing/hash";
        public const string getSignedStatus = "external/esrm/service/signing/api/v1/Signing/status";
        public const string attachDocument = "external/esrm/service/document/api/v1/documents/attachment";

        public static string DynamicUrl(this string url, string dynamicParam)
        {
            return url + "/" + dynamicParam;    
        }
    }
}
