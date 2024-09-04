using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GatewayServiceTest.Signature
{
    public class VerifyFile
    {
        //
        private static readonly log4net.ILog _log = log4net.LogManager.GetLogger(typeof(SignHash));
        //
        private const string _pdfInput = @"E:/WORK/2018/07-2018/signed.pdf";
        //
        private const string _officeInput = @"E:/WORK/2018/07-2018/signed.docx";
        //
        private const string _xmlInput = @"E:/WORK/2018/07-2018/signed.xml";

        public static void VerifyPdf(string access_token)
        {
            // Get hash value from unsigned data ---------------------------------------------------
            byte[] signedData = null;
            try
            {
                signedData = File.ReadAllBytes(_pdfInput);
            }
            catch (Exception ex)
            {
                _log.Error(ex);
                return;
            }
            var response = _verify("signed.pdf", "application/pdf", "pdf", Convert.ToBase64String(signedData), access_token);

            if(response == null)
            {
                _log.Error("Cannot verify signed data");
                return;
            }
            _log.Info("Signature valid: " + response.status);
            _log.Info("Result message: " + response.message);
            foreach(var signature in response.signatures)
            {
                _log.Info("Signature " + signature.signatureIndex + " response code: " + signature.code);
                _log.Info("Signature " + signature.signatureIndex + " valid: " + signature.signatureStatus);
                _log.Info("Signature " + signature.signatureIndex + " time: " + signature.signingTime);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="contentType"></param>
        /// <param name="type"></param>
        /// <param name="_dataBase64"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        private static VerifyResultModel _verify(string fileName, string contentType, string type, string _dataBase64, string token)
        {
            var response = CoreServiceClient.Query(new RequestMessage
            {
                RequestID = Guid.NewGuid().ToString(),
                ServiceID = "SignServer",
                FunctionName = "Verify",
                Parameter = new SignParameter
                {
                    Type = type,
                    FileName = fileName,
                    ContentType = contentType,
                    DataBase64 = _dataBase64
                }
            }, token);
            if (response != null)
            {
                var str = JsonConvert.SerializeObject(response.Content);
                VerifyResultModel acc = JsonConvert.DeserializeObject<VerifyResultModel>(str);
                if (acc != null)
                {
                    return acc;
                }
            }

            return null;
        }
    }
}
