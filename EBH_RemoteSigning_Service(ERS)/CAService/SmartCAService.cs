using EBH_RemoteSigning_Service_ERS.clsUtilities;
using EBH_RemoteSigning_Service_ERS.Request;
using EBH_RemoteSigning_Service_ERS.Response;
using EBH_RemoteSigning_Service_ERS_.Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Web;

namespace EBH_RemoteSigning_Service_ERS.CAService
{
    public class SmartCAService : ISmartCAService
    {
        private ConfigRequest _configRequest;

        public SmartCAService(ConfigRequest configRequest)
        {
            _configRequest = configRequest;   
        }


        public UserCertificate GetAccountCert(String uri, string serialNumber = "")
        {
            var response = MethodLibrary.Query(new ReqGetCert
            {
                sp_id = _configRequest.sp_id,
                sp_password = _configRequest.sp_password,
                user_id = _configRequest.uid,
                serial_number = "",
                transaction_id = Guid.NewGuid().ToString(),
            }, uri);
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
                    if(returnCert != null)
                    {
                        return returnCert;
                    }
                    return  res.data.user_certificates[0];
                }
                else
                {
                    return null;
                }

            }
            return null;

        }

        public List<UserCertificate> GetListAccountCert(String uri)
        {
            var response = MethodLibrary.Query(new ReqGetCert
            {
                sp_id = _configRequest.sp_id,
                sp_password = _configRequest.sp_password,
                user_id = _configRequest.uid,
                serial_number = "",
                transaction_id = Guid.NewGuid().ToString(),
            }, uri);
            if (response != null)
            {
                ResGetCert res = JsonConvert.DeserializeObject<ResGetCert>(response);

                if (res.data.user_certificates.Count() >= 1)
                {
                    return res.data.user_certificates;
                }
                else
                {
                    return null;
                }

            }
            return null;

        }

        public ResSign Sign(String uri, List<SignFile> sign_files, String serialNumber)
        {
            var response = MethodLibrary.Query(new ReqSign
            {
                sp_id = _configRequest.sp_id,
                sp_password = _configRequest.sp_password,
                user_id = _configRequest.uid,
                transaction_id = Guid.NewGuid().ToString(),
                transaction_desc = "Sign request from eBH",
                sign_files = sign_files,
                serial_number = serialNumber,

            }, uri);
            if (response != null)
            {
                ResSign req = JsonConvert.DeserializeObject<ResSign>(response);
                return req;
            }
            return null;
        }

        public DataTransaction GetStatus(String uri)
        {
            var response = Query(new Object
            {
            }, uri);
            if (response != null)
            {
                ResStatus res = JsonConvert.DeserializeObject<ResStatus>(response);
                return res.data;
            }
            return null;
        }
    }
}