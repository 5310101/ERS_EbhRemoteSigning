using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Threading;

namespace ERS_Domain
{
    public enum HttpMethodType
    {
        get,
        post,
        put,
        delete,
    }
    public class HttpSendRequest
    {
        private readonly HttpClient _client;
        public Dictionary<string, string> Header { get; set; } = new Dictionary<string, string>();
        public string AuthorizeToken { get; set; }
        private HttpMethodType _method { get; set; }

        public HttpSendRequest()
        {
            //chi tao httpclient 1 lan  
            _client = new HttpClient();
        }

        public void SetAuthorizeToken(string token)
        {
            AuthorizeToken = token;
        }

        public void SetHeader(Dictionary<string, string> headers)
        {
            Header = headers;
        }

        public async Task<TRes> SendRequestAsync<TRes>(HttpMethodType method, string url, object body = null, CancellationToken cancellationToken = default)
        {
            //try
            //{
            string bodyJson = JsonConvert.SerializeObject(body);
            var bodyContent = new StringContent(bodyJson, Encoding.UTF8, "application/json");

            if (Header.Count > 0)
            {
                _client.DefaultRequestHeaders.Clear();
                foreach (KeyValuePair<string, string> pair in Header)
                {
                    _client.DefaultRequestHeaders.Add(pair.Key, pair.Value);
                }
            }
            if (!string.IsNullOrEmpty(AuthorizeToken))
            {
                _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AuthorizeToken);
            }

            HttpResponseMessage res = null;
            try
            {
                switch (method)
                {
                    case HttpMethodType.get:
                        res = await _client.GetAsync(url, cancellationToken).ConfigureAwait(false);
                        break;
                    case HttpMethodType.post:
                        res = await _client.PostAsync(url, bodyContent, cancellationToken).ConfigureAwait(false);
                        break;
                    case HttpMethodType.put:
                        res = await _client.PutAsync(url, bodyContent, cancellationToken).ConfigureAwait(false);
                        break;
                    case HttpMethodType.delete:
                        res = await _client.DeleteAsync(url, cancellationToken).ConfigureAwait(false);
                        break;
                    default:
                        throw new Exception("Phương thức không được hỗ trợ");
                }
            }
            catch (OperationCanceledException)
            {
                Utilities.logger.ErrorLog(new Exception("Request timeout"), "HttpSendRequest");
                return default;
            }
            

            res.EnsureSuccessStatusCode();
            string resJson = await res.Content.ReadAsStringAsync().ConfigureAwait(false);
            return JsonConvert.DeserializeObject<TRes>(resJson);
        }
        //catch (Exception ex)
        //{
        //    //ghi log
        //    return default(TRes);
        //}

    }
}
