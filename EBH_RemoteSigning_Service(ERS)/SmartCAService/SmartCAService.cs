using EBH_RemoteSigning_Service_ERS_.clsUtilities;
using EBH_RemoteSigning_Service_ERS_.Request;
using EBH_RemoteSigning_Service_ERS_.Response;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Web;

namespace EBH_RemoteSigning_Service_ERS_.SmartCAService
{
    public class SmartCAService : ISmartCAService
    {
        private ConfigRequest _configRequest;

        public SmartCAService()
        {
            _configRequest = new ConfigRequest();   
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

        public DataSign Sign(String uri, string data_to_be_signed, String serialNumber)
        {


            var sign_files = new List<SignFile>();
            var sign_file = new SignFile();
            sign_file.data_to_be_signed = data_to_be_signed;
            sign_file.doc_id = data_to_be_signed;
            sign_file.file_type = "pdf";
            sign_file.sign_type = "hash";
            sign_files.Add(sign_file);
            var response = Query(new ReqSign
            {
                sp_id = client_id,
                sp_password = client_secret,
                user_id = uid,
                transaction_id = Guid.NewGuid().ToString(),
                transaction_desc = "Ký Test từ NgoQuangDat",
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