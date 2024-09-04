using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using VnptHashSignatures.Interface;
using VnptHashSignatures.Pdf;

namespace GatewayServiceTest.Signature
{
    public class SignMultipleHash
    {
        //
        private static readonly log4net.ILog _log = log4net.LogManager.GetLogger(typeof(SignHash));
        //
        private const string _pdfInput = @"F:/WORK/2018/07-2018/input.pdf";

        public static void SignMultipleHashs(string groupId, string certId, string certBase64, string access_token)
        {
            List<byte[]> data = new List<byte[]>();
            for(int i = 0; i < 10; i++)
            {
                data.Add(File.ReadAllBytes(_pdfInput));
            }
            var signer = new PdfMultiHashSigner(data, certBase64);
            var hashValue = signer.GetHashValue();
            var externalSignature = _signMultipleHash(Convert.ToBase64String(hashValue), "pdf", 
                "application/pdf", certId, groupId, access_token);
            var signeds = signer.Sign(externalSignature);
            _log.Info($"Signed {signeds.Count} files");
        }
        /// 
        /// </summary>
        /// <param name="hashValue"></param>
        /// <param name="type"></param>
        /// <param name="contentType"></param>
        /// <param name="certId"></param>
        /// <param name="groupId"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        private static string _signMultipleHash(string hashValue, string type, string contentType, string certId, string groupId, string token)
        {
            var response = CoreServiceClient.Query(new RequestMessage
            {
                RequestID = Guid.NewGuid().ToString(),
                ServiceID = "SignServer",
                FunctionName = "SignHashMultiple",
                Parameter = new SignParameter
                {
                    CertID = certId,
                    ServiceGroupID = groupId,
                    Type = type,
                    ContentType = contentType,
                    DataBase64 = hashValue
                }
            }, token);
            if (response != null)
            {
                if(response.ResponseCode == 53)
                {
                    // TODO: Xử lý 2FA
                }
                else
                {
                    var str = JsonConvert.SerializeObject(response.Content);
                    SignResponse acc = JsonConvert.DeserializeObject<SignResponse>(str);
                    if (acc != null)
                    {
                        return acc.SignedData;
                    }
                }
            }

            return null;
        }
    }
}
