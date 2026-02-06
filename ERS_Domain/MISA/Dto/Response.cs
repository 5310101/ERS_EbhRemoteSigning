using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERS_Domain.MISA.Dto
{
    public class MISAResponseError
    {
        public string error { get; set; }
        public string errorCode { get; set; }
        public string devMsg { get; set; }
        public string userMsg { get; set; }
    }

    public class Data
    {
        public string accessToken { get; set; }
        public string remoteSigningAccessToken { get; set; }
        public string tokenType { get; set; }
        public int expiresIn { get; set; }
        public string refreshToken { get; set; }
        public User user { get; set; }
        public Default @default { get; set; }
        public VerifyUser verifyUser { get; set; }
    }

    public class Default
    {
        public bool email { get; set; }
        public bool phoneNumber { get; set; }
        public bool appAuthenticator { get; set; }
    }

    public class MISASigninResponse
    {
        public Status status { get; set; }
        public Data data { get; set; }
    }

    public class Status
    {
        public string type { get; set; }
        public int code { get; set; }
        public string message { get; set; }
        public int error { get; set; }
        public int errorCode { get; set; }
        public string devMsg { get; set; }
        public string userMsg { get; set; }
    }

    public class User
    {
        public string id { get; set; }
        public string email { get; set; }
        public string phoneNumber { get; set; }
        public string firstName { get; set; }
        public string lastName { get; set; }
        public string username { get; set; }
    }

    public class VerifyUser
    {
        public bool emailsVerify { get; set; }
        public bool phoneNumberIsVerify { get; set; }
        public bool isChangePassword { get; set; }
    }

    public class MISARefreshTokenResponse
    {
        public string remoteSigningAccessToken { get; set; }
        public string accessToken { get; set; }
        public string refreshToken { get; set; }
        public int expiresIn { get; set; }
    }

    public class MISACertificate
    {
        public string userId { get; set; }
        public string keyAlias { get; set; }
        public string appName { get; set; }
        public string keyStatus { get; set; }
        public string certificate { get; set; }
        public List<string> certiticateChain { get; set; }
    }

}
