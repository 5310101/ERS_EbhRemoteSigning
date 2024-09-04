using log4net;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace GatewayServiceTest
{
    /// <summary>
    /// Gateway API request handler
    /// </summary>
    public class CoreServiceClient
    {
        // Logger for this class
        private static readonly ILog _log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Request for protected resource with access_token
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public static ResponseMessage Query(RequestMessage req, string accessToken)
        {
            // Verify service certificate. 
            ServicePointManager.ServerCertificateValidationCallback 
                += new RemoteCertificateValidationCallback(SslHelper.ValidateRemoteCertificate);

            // Create RestClient instance and add client certificate
            var serviceUri = Properties.Settings.Default["SERVICE_URL"] as string;
            RestClient client = new RestClient(serviceUri);

            RestRequest request = new RestRequest(Method.POST)
            {
                RequestFormat = DataFormat.Json
            };
            request.AddJsonBody(req);
            request.AddHeader("Authorization", $"Bearer {accessToken}");

            IRestResponse response = null;
            try
            {
                response = client.Execute(request);
            }
            catch (Exception ex)
            {
                _log.Error($"Connect gateway error: {ex.Message}", ex);
            }

            if (response == null || response.ErrorException != null)
            {
                _log.Error("Service return null response");
                return null;
            }
            if(response.StatusCode != HttpStatusCode.OK)
            {
                _log.Error($"Status code={response.StatusCode}. Status content: {response.Content}");
                return null;
            }

            var respContent = response.Content;
            try
            {
                return JsonConvert.DeserializeObject<ResponseMessage>(respContent);
            }
            catch (Exception ex)
            {
                _log.Error($"Service return error: {ex.Message}", ex);
                return null;
            }
        }

        public static String Query(object req, string serviceUri, string accessToken)
        {
            // Verify service certificate. 
            ServicePointManager.ServerCertificateValidationCallback
                += new RemoteCertificateValidationCallback(SslHelper.ValidateRemoteCertificate);
            
            RestClient client = new RestClient(serviceUri);
            RestRequest request = new RestRequest(Method.POST)
            {
                RequestFormat = DataFormat.Json
            };
            request.AddJsonBody(req);
            request.AddHeader("Authorization", $"Bearer {accessToken}");
            IRestResponse response = null;
            try
            {
                response = client.Execute(request);
            }
            catch (Exception ex)
            {
                _log.Error($"Connect gateway error: {ex.Message}", ex);
            }

            if (response == null || response.ErrorException != null)
            {
                _log.Error("Service return null response");
                return null;
            }
            if (response.StatusCode != HttpStatusCode.OK)
            {
                _log.Error($"Status code={response.StatusCode}. Status content: {response.Content}");
                return null;
            }

            return response.Content;            
        }

        /// <summary>
        /// Request new access_token from long live refresh_token
        /// </summary>
        /// <param name="refreshToken">refresh_token from previous access_token request</param>
        /// <param name="newRefreshToken">return new refresh_token</param>
        /// <returns>new access_token</returns>
        public static string RefreshToken(string refreshToken, out string newRefreshToken)
        {
            newRefreshToken = "";
            // Verify service certificate. 
            ServicePointManager.ServerCertificateValidationCallback 
                += new RemoteCertificateValidationCallback(SslHelper.ValidateRemoteCertificate);

            // Create RestClient instance and add client certificate
            var tokenUri = Properties.Settings.Default["SERVICE_GET_TOKENURL"] as string;
            RestClient client = new RestClient(tokenUri);

            // Token request body
            RestRequest request = new RestRequest(Method.POST);
            var req = new GetTokenBody
            {
                grant_type = "refresh_token",
                client_id = Properties.Settings.Default["APP_ID"] as string,
                client_secret = Properties.Settings.Default["APP_SECRET"] as string,
            };
            var param = JsonConvert.SerializeObject(req);
            _log.Info("access_token request:" + param);

            request.AddHeader("content-type", "application/x-www-form-urlencoded");
            request.AddParameter("application/x-www-form-urlencoded", 
                $"grant_type={req.grant_type}" 
                + $"&client_id={req.client_id}" 
                + $"&client_secret={req.client_secret}" 
                + $"&refresh_token={refreshToken}", 
                ParameterType.RequestBody);

            IRestResponse response = null;
            try
            {
                response = client.Execute(request);                
                
            }
            catch (Exception ex)
            {
                _log.Error($"Connect gateway error: {ex.Message}", ex);
            }
            if (response == null || response.ErrorException != null)
            {
                _log.Error("Service return null response");
                return null;
            }            
            if (response.StatusCode != HttpStatusCode.OK)
            {
                _log.Error($"Status code={response.StatusCode}. Status content: {response.Content}");
                return null;
            }

            var respContent = response.Content;
            try
            {
                var resp = JsonConvert.DeserializeObject<GetTokenResponse>(respContent);
                _log.Info("access_token:" + resp.access_token);
                _log.Info($"refresh_token: {resp.refresh_token}");
                newRefreshToken = resp.refresh_token;
                return resp.access_token;
            }
            catch (Exception ex)
            {
                _log.Error($"Service return error: {ex.Message}", ex);
                return null;
            }
        }

        /// <summary>
        /// Resource Owner Password grant.
        /// Send user email/password to get access_token
        /// </summary>
        /// <param name="email"></param>
        /// <param name="pass"></param>
        /// <returns></returns>
        public static string GetAccessToken(string email, string pass, out string refresh_token)
        {
            refresh_token = "";
            // Verify service certificate. 
            ServicePointManager.ServerCertificateValidationCallback 
                += new RemoteCertificateValidationCallback(SslHelper.ValidateRemoteCertificate);

            // Create RestClient instance and add client certificate
            var tokenUri = Properties.Settings.Default["SERVICE_GET_TOKENURL"] as string;
            RestClient client = new RestClient(tokenUri);

            // Token request body
            RestRequest request = new RestRequest(Method.POST);
            var req = new GetTokenBody
            {
                grant_type = "password",
                username = email,
                password = pass,
                client_id = Properties.Settings.Default["APP_ID"] as string,
                client_secret = Properties.Settings.Default["APP_SECRET"] as string,
            };
            var param = JsonConvert.SerializeObject(req);
            _log.Info("access_token request:" + param);

            request.AddHeader("content-type", "application/x-www-form-urlencoded");
            request.AddParameter("application/x-www-form-urlencoded", $"grant_type={req.grant_type}" +
                $"&username={req.username}&password={req.password}&client_id={req.client_id}" +
                $"&client_secret={req.client_secret}", ParameterType.RequestBody);
            // End: Token request body

            IRestResponse response = null;
            try
            {
                response = client.Execute(request);
            }
            catch (Exception ex)
            {
                _log.Error($"Connect gateway error: {ex.Message}", ex);
            }

            if (response == null || response.ErrorException != null)
            {
                _log.Error("Service return null response");
                return null;
            }
            Encoding encoding = Encoding.GetEncoding("ISO-8859-1");
            var result = encoding.GetString(response.RawBytes);
            if (response.StatusCode != HttpStatusCode.OK)
            {
                _log.Error($"Status code={response.StatusCode}. Status content: {response.Content}");
                return null;
            }

            var respContent = response.Content;
            try
            {
                var resp = JsonConvert.DeserializeObject<GetTokenResponse>(respContent);
                _log.Info("access_token:" + resp.access_token);
                _log.Info($"refresh_token: {resp.refresh_token}");
                refresh_token = resp.refresh_token;
                return resp.access_token;
            }
            catch (Exception ex)
            {
                _log.Error($"Service return error: {ex.Message}", ex);
                return null;
            }
        }

        /// <summary>
        /// access_token request parameter mapping
        /// </summary>
        class GetTokenBody
        {
            public string grant_type { get; set; }
            public string username { get; set; }
            public string password { get; set; }
            public string client_id { get; set; }
            public string client_secret { get; set; }
        }

        /// <summary>
        /// access_token response mapping
        /// </summary>
        class GetTokenResponse
        {
            // access_token value
            public string access_token { get; set; }
            // refresh_token to get new access_token (see RefreshToken method)
            public string refresh_token { get; set; }
            public string token_type { get; set; }
            // access_token valid time. when expired, using refresh_token to get new or require user re-authorize
            public int expires_in { get; set; }
        }
    }
}