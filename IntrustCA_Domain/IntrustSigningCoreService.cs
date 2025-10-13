using eSDK;
using IntrustCA_Domain.Dtos;
using IntrustCA_Domain.CreateAppDomain;
using Newtonsoft.Json;
using System;

namespace IntrustCA_Domain
{
    public static class IntrustSigningCoreService
    {
        //Set path Config (Instrust) ko can lam
        public static void SetPathConfig()
        {
            rms.lib.common.library.Publib._pathToFileConfig = AppDomain.CurrentDomain.BaseDirectory;
        }

        /// <summary>
        /// Create a signing session, which is required before signing. After that, the signature is no longer confirmed via the mobile app
        /// </summary>
        /// <param name="req"></param>
        /// <returns></returns>
        public static SessionRegisterResponse RegisterSession(SessionRegisterRequest req)
        {
            string jsonData = JsonConvert.SerializeObject(req);
            //string jsonResponse = Signer.registerSessionPub(jsonData);
            string jsonResponse = ESDKCaller.Call("registerSessionPub", jsonData);
            return JsonConvert.DeserializeObject<SessionRegisterResponse>(jsonResponse);   
        }

        /// <summary>
        /// get certificate info
        /// </summary>
        /// <param name="req"></param>
        /// <returns></returns>
        public static GetCertificateResponse GetCertificate(GetCertificateRequest req)
        {
            string jsonData = JsonConvert.SerializeObject(req);
            //string jsonstrResponse = Signer.ect_get_certificates(jsonData);
            string jsonstrResponse = ESDKCaller.Call("ect_get_certificates", jsonData);
            return JsonConvert.DeserializeObject<GetCertificateResponse>(jsonstrResponse);
        }

        /// <summary>
        /// sign document remotely
        /// </summary>
        /// <param name="req"></param>
        /// <returns></returns>
        public static SignResponse SignRemote(SignRequest req)
        {
            string jsonData = JsonConvert.SerializeObject(req, Formatting.Indented);
            //string jsonResponse1 = Signer.signRMS(jsonData);
            string jsonResponse = ESDKCaller.Call("signRMS",jsonData);
            return JsonConvert.DeserializeObject<SignResponse>(jsonResponse);
        }

        /// <summary>
        /// extend login session
        /// </summary>
        /// <param name="req"></param>
        /// <returns></returns>
        public static ExtendLoginResponse ExtendLogin(ExtendLoginRequest req)
        {
            string jsonData = JsonConvert.SerializeObject(req);
            //string jsonResponse = Signer.login(jsonData);
            string jsonResponse = ESDKCaller.Call("login",jsonData);
            return JsonConvert.DeserializeObject<ExtendLoginResponse>(jsonResponse);
        }   
    }
}
