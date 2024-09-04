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
    public class SmartCAService
    {
        public static UserCertificate GetAccountCert(String uri)
        {
            var response = Query(new ReqGetCert
            {
                sp_id = client_id,
                sp_password = client_secret,
                user_id = uid,
                serial_number = "",
                transaction_id = "321"
            }, uri);
            if (response != null)
            {
                ResGetCert res = JsonConvert.DeserializeObject<ResGetCert>(response);

                if (res.data.user_certificates.Count() == 1)
                {
                    return res.data.user_certificates[0];
                }
                else if (res.data.user_certificates.Count() > 1)
                {
                    for (int i = 0; i < res.data.user_certificates.Count(); i++)
                    {
                        Console.WriteLine("--------------");
                        Console.WriteLine("Certificate index : " + i);
                        Console.WriteLine("service_type : " + res.data.user_certificates[i].service_type);
                        Console.WriteLine("service_name : " + res.data.user_certificates[i].service_name);
                        Console.WriteLine("serial_number : " + res.data.user_certificates[i].serial_number);
                        Console.WriteLine("cert_subject : " + res.data.user_certificates[i].cert_subject);
                        Console.WriteLine("cert_valid_from : " + res.data.user_certificates[i].cert_valid_from);
                        Console.WriteLine("cert_valid_to : " + res.data.user_certificates[i].cert_valid_to);
                    }
                    Console.WriteLine("Choose Certificate index :");
                    String certIndex = Console.ReadLine();
                    int certIn;
                    bool isNumber = int.TryParse(certIndex, out certIn);
                    if (!isNumber)
                    {
                        return null; ;
                    }
                    if (certIn < 0 || certIn >= res.data.user_certificates.Count())
                    {
                        return null;
                    }
                    return res.data.user_certificates[certIn];

                }
                else
                {
                    return null;
                }

            }
            return null;

        }

        public static DataSign Sign(String uri, string data_to_be_signed, String serialNumber)
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

        public static DataTransaction _getStatus(String uri)
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