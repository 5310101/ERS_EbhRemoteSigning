using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace GatewayServiceTest.Signature
{
    [Obsolete("Not use any more...", true)]
    public class MultipleCMS
    {
        // Logger for this class
        private static readonly log4net.ILog _log = log4net.LogManager.GetLogger(typeof(MultipleCMS));

        // Sample input/output file ppath
        private const string _pdfInput = @"E:/WORK/2018/07-2018/input.pdf";
        private const string _pdfInput1 = @"E:/WORK/2018/07-2018/input1.pdf";
        private const string _pdfSignedPath = @"E:/WORK/2018/07-2018/signed.pdf";

        /// <summary>
        /// Sign multiple file in 1 request
        /// </summary>
        /// <param name="certId"></param>
        /// <param name="access_token"></param>
        public static void SignMultiple(string certId, string access_token)
        {
            _log.Info("Sign multiple hash: Init signer...");
            var startTime = DateTime.Now;

            // 1. init unsign data list
            var files = new List<string>();
            for (var i = 0; i < 100; i++)
            {
                files.Add(_pdfInput1);
            }

            List<HashEntity> entities = new List<HashEntity>();
            var fileSize = 0;
            foreach (var file in files)
            {
                var unsignBytes = System.IO.File.ReadAllBytes(file);
                if (unsignBytes == null)
                {
                    continue;
                }
                fileSize = unsignBytes.Length;
                var secondHash = Convert.ToBase64String(unsignBytes);
                entities.Add(new HashEntity
                {
                    Data = secondHash
                });
            }
            var data = JsonConvert.SerializeObject(entities);
            var unsignData = Encoding.GetEncoding("UTF-8").GetBytes(data);
            var serviceStartTime = DateTime.Now;
            _log.Info("Sign multiple hash: Calling signservice...");
            var externalSignature = _signMultipleHash(Convert.ToBase64String(unsignData), "pdf", "application/pdf", certId,  access_token);
            var serviceEndTime = DateTime.Now;
            _log.Info("File size: " + fileSize);
            _log.Info("Service time: " + (serviceEndTime - serviceStartTime).TotalSeconds);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="hashValue"></param>
        /// <param name="type"></param>
        /// <param name="contentType"></param>
        /// <param name="certId"></param>
        /// <param name="groupId"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        private static string _signMultipleHash(string hashValue, string type, string contentType, string certId, string token)
        {
            var response = CoreServiceClient.Query(new RequestMessage
            {
                RequestID = Guid.NewGuid().ToString(),
                ServiceID = "SignServer",
                FunctionName = "SignHashMultiple",
                Parameter = new SignParameter
                {
                    CertID = certId,
                    Type = type,
                    ContentType = contentType,
                    DataBase64 = hashValue
                }
            }, token);
            if (response != null)
            {
                var str = JsonConvert.SerializeObject(response.Content);
                SignResponse acc = JsonConvert.DeserializeObject<SignResponse>(str);
                if (acc != null)
                {
                    return acc.SignedData;
                }
            }

            return null;
        }

        class HashEntity
        {
            public string Data { get; set; }
        }
        class HashSigned
        {
            public string Data { get; set; }
            public string Signature { get; set; }
            public int Code { get; set; }
        }
    }
}
