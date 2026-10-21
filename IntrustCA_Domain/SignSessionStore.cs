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

        //nen dat lon hon thoi gian cho xac nhan cua app Intrust mot chut, tranh bo cho truoc khi user kip xac nhan
        private static int RegisterTimeoutSeconds
        {
            get
            {
                int seconds;
                return int.TryParse(ConfigurationManager.AppSettings["SESSION_REGISTER_TIMEOUT"], out seconds) ? seconds : 180;
            }
        }
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
            CreateSession();
        }

        private void CreateSession()
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

                SessionRegisterResponse res;
                try
                {
                    res = IntrustSigningCoreService.RegisterSession(registerRequest, RegisterTimeoutSeconds * 1000);
                }
                catch (TimeoutException ex)
                {
                    throw new SessionRejectedException($"Hết thời gian chờ xác nhận phiên ký trên app Intrust ({RegisterTimeoutSeconds}s)", ex);
                }
                //Intrust da tra loi ro rang (user tu choi, het han xac nhan, sai thong tin...) => retry ngay chi lam user bi hoi lai
                if (res.status != "success")
                {
                    throw new SessionRejectedException($"Không tạo được phiên ký (người dùng từ chối/không xác nhận hoặc thông tin không hợp lệ): [{res.status_code}] {res.error_desc}");
                }
                AuthData = res.auth_data;
                RefreshToken = res.refresh_token;
                //luon lay truoc khi het han that su cua phien 5 p
                ValidUntil = DateTime.Now.AddMinutes(SessionTime - 5);
            }
            catch (SessionRejectedException ex)
            {
                Logger.ErrorLog(ex, "Create session rejected", "SignSessionStore");
                throw;
            }
            catch (Exception ex)
            {
                //log kem loi goc de biet ly do (loi mang, loi eSDK...)
                Logger.ErrorLog(ex, "Create session failed", "SignSessionStore");
                throw new Exception("Create session failed: " + ex.Message, ex);
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
            catch (Exception ex)
            {
                //log
                Logger.ErrorLog(ex, "AutoRefresh session failed", "SignSessionStore");
                return false;
            }
        }
    }
}
