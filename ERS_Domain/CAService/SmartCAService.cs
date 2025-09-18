using ERS_Domain.clsUtilities;
using ERS_Domain.Request;
using ERS_Domain.Response;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography;

namespace ERS_Domain.CAService
{
    public class SmartCAService : IRemoteSignService
    {
        private ConfigRequest _configRequest;

        public SmartCAService(ConfigRequest configRequest)
        {
            _configRequest = configRequest;
        }


        public UserCertificate GetAccountCert(string uri, string uid, string serialNumber = "")
        {
            try
            {
                var response = MethodLibrary.Query(new ReqGetCert
                {
                    sp_id = _configRequest.sp_id,
                    sp_password = _configRequest.sp_password,
                    user_id = uid,
                    serial_number = "",
                    transaction_id = Guid.NewGuid().ToString(),
                }, uri);
                //Utilities.logger.ErrorLog("response",response.SafeString());
                //Utilities.logger.ErrorLog("param", $"{_configRequest.sp_id},{_configRequest.sp_password},{uid},{ serialNumber}");
                if (response != null)
                {
                    ResGetCert res = JsonConvert.DeserializeObject<ResGetCert>(response);

                    if (res.data.user_certificates.Count() == 1 || serialNumber == "")
                    {
                        return res.data.user_certificates[0];
                    }
                    else if (res.data.user_certificates.Count() > 1)
                    {
                        var returnCert = res.data.user_certificates.First((c) => c.serial_number == serialNumber);
                        if (returnCert != null)
                        {
                            return returnCert;
                        }
                        return res.data.user_certificates[0];
                    }
                    else
                    {
                        return null;
                    }

                }
                return null;

            }
            catch (Exception ex)
            {
                Utilities.logger.ErrorLog(ex, "GetAccountCert", uid);
                return null;
            }
        }

        public UserCertificate[] GetListAccountCert(string uri, string uid)
        {
            try
            {
                var response = MethodLibrary.Query(new ReqGetCert
                {
                    sp_id = _configRequest.sp_id,
                    sp_password = _configRequest.sp_password,
                    user_id = uid,
                    serial_number = "",
                    transaction_id = Guid.NewGuid().ToString(),
                }, uri);
                if (response != null)
                {
                    ResGetCert res = JsonConvert.DeserializeObject<ResGetCert>(response);

                    if (res.data.user_certificates.Count() >= 1)
                    {
                        return res.data.user_certificates.ToArray();
                    }
                    else
                    {
                        return null;
                    }

                }
                return null;

            }
            catch (Exception ex)
            {
                Utilities.logger.ErrorLog(ex, "GetListAccountCert", uid);
                return null;
            }

        }

        public DataSign Sign(string uri, string data_to_be_signed, string serialNumber, string uid, string fileType)
        {

            var sign_files = new List<SignFile>();
            var sign_file = new SignFile();
            sign_file.data_to_be_signed = data_to_be_signed;
            sign_file.doc_id = data_to_be_signed;
            sign_file.file_type = fileType;
            sign_file.sign_type = "hash";
            sign_files.Add(sign_file);
            var response = Query(new ReqSign
            {
                sp_id = _configRequest.sp_id,
                sp_password = _configRequest.sp_password,
                user_id = uid,
                transaction_id = Guid.NewGuid().ToString(),
                transaction_desc = "Ký từ EBH",
                sign_files = sign_files,
                serial_number = serialNumber,

            }, uri);
            if (response != null)
            {
                ResSign req = JsonConvert.DeserializeObject<ResSign>(response);
                return req.data;
            }
            return null;
        }

        private String Query(object req, string serviceUri)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            //ServicePointManager.ServerCertificateValidationCallback
            //    += new RemoteCertificateValidationCallback(SslHelper.ValidateRemoteCertificate);

            RestClient client = new RestClient(serviceUri);
            RestRequest request = new RestRequest(Method.POST);
            request.AddHeader("Content-Type", "application/json");
            var body = JsonConvert.SerializeObject(req);
            request.AddParameter("application/json", body, ParameterType.RequestBody);
            IRestResponse response = null;
            try
            {
                response = client.Execute(request);
            }
            catch (Exception ex)
            {
                Utilities.logger.ErrorLog(ex, $"Connect gateway error: {ex.Message}");
                throw new Exception($"Connect gateway error: {ex.Message}");
            }

            if (response == null || response.ErrorException != null)
            {
                Utilities.logger.ErrorLog("Service return null response", "Server error");
                throw new Exception("Server error: Service return null response");
            }
            if (response.StatusCode != HttpStatusCode.OK)
            {
                Utilities.logger.ErrorLog($"Status code={response.StatusCode}. Status content: {response.Content}", "Server error");
                throw new Exception($"Server error: Status code={response.StatusCode}. Status content: {response.Content}");
            }

            return response.Content;
        }

        public ResStatus GetStatus(string uri)
        {
            var response = MethodLibrary.Query(new Object
            {
            }, uri);
            if (response != null)
            {
                return JsonConvert.DeserializeObject<ResStatus>(response);
            }
            return null;
        }
    }
}