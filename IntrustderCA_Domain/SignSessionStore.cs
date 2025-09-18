using IntrustCA_Domain;
using IntrustCA_Domain.Dtos;
using System;
using System.Configuration;

namespace IntrustCA_Domain
{
    public class SignSessionStore
    {
        public string UserName { get; }
        public string SessionId { get; private set; }
        public string RefreshToken { get; private set; }
        public string AuthData { get; private set; }

        public DateTime ValidUntil { get; private set; }

        public int SessionTime => int.Parse(ConfigurationManager.AppSettings["SessionTime"]);
        public ICACertificate Cert { get; }

        public bool IsSessionValid
        {
            get
            {
                if(DateTime.Now >= ValidUntil)
                {
                    return AutoRefresh();
                }
                return true;
            }
        }

        public SignSessionStore(string UserName , ICACertificate Certificate)
        {
            this.UserName = UserName;
            this.Cert = Certificate;
            this.SessionId = Guid.NewGuid().ToString();
            if (CreateSession() == false)
            {
                throw new Exception("Create session failed");
            }
        }

        private bool CreateSession()
        {
            try
            {
                var registerRequest = new SessionRegisterRequest()
                {
                    user_name = UserName,
                    credentialID = Cert.key_id,
                    session_time = SessionTime,
                    is_use_request_login = true,
                    auth_data = ""
                };

                var res = IntrustSigningCoreService.RegisterSession(registerRequest);
                if (res.status != "success")
                {
                    throw new Exception("Register session failed: " + res.error_desc);
                }
                AuthData = res.auth_data;
                RefreshToken = res.refresh_token;
                //luon lay truoc khi het han that su cua phien 5 p
                ValidUntil = DateTime.Now.AddMinutes(SessionTime - 5);
                return true;
            }
            catch (Exception)
            {
                //log
                return false;
            }
        }

        //cho phep goi tu dong ben trong lan goi tu ben ngoai
        public bool AutoRefresh()
        {
            try
            {
                //khi het thoi han phien thi tu dong gia han
                var req = new ExtendLoginRequest()
                {
                    user_name = UserName,
                    is_use_pin_code = false,
                    is_use_request_login = true,
                    is_get_token_by_refresh = true,
                    refresh_token = RefreshToken,
                    auth_data = AuthData
                };
                var res = IntrustSigningCoreService.ExtendLogin(req);
                if (res.status != "success")
                {
                    throw new Exception("Extend session failed: " + res.error_desc);
                }
                AuthData = res.auth_data;
                ValidUntil = DateTime.Now.AddMinutes(SessionTime - 5);
                return true;
            }
            catch (Exception)
            {
                //log
                return false;
            }
        }
    }
}
