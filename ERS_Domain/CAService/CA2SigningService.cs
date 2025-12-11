using ERS_Domain.clsUtilities;
using ERS_Domain.Request;
using ERS_Domain.Response;
using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace ERS_Domain.CAService
{
    public class CA2SigningService
    {
        private readonly CA2ConfigRequest _config;
        private readonly HttpSendRequest _sendService;
        public CA2SigningService()
        {
            _config = new CA2ConfigRequest();
            _sendService = new HttpSendRequest();   
        }

        public async Task<CA2Response<CA2Certificates>> GetCertificates(string userId, string transactionId, string serialNumber = "", CancellationToken cancellationToken = default)
        {
            CA2GetCertRequest request = new CA2GetCertRequest
            {
                sp_id = _config.sp_id,
                sp_password = _config.sp_password,
                user_id = userId,
                serial_number = serialNumber,
                transaction_id = transactionId,
            };
            return await _sendService.SendRequestAsync<CA2Response<CA2Certificates>>(HttpMethodType.post ,CA2_URI.uriGetCert, request, cancellationToken);
        } 

        public async Task<CA2Response<FileSigned>> SignHashValue(string userId, string transactionId, FileToSign[] lstFile, string serialNumber, DateTime signTime)
        {
            CA2SignRequest request = new CA2SignRequest
            {
                sp_id = _config.sp_id,
                sp_password = _config.sp_password,
                user_id = userId,
                transaction_id = transactionId,
                serial_number = serialNumber,
                sign_files = lstFile,
                time_stamp = signTime.ToString("yyyyMMddHHmmss"),
            };
            return await _sendService.SendRequestAsync<CA2Response<FileSigned>>(HttpMethodType.post, CA2_URI.uriSign, request);
        }

        public async Task<CA2StatusCheckResponse> GetSignedResult(string userId, string transactionId)
        {
            CA2StatusCheckRequest request = new CA2StatusCheckRequest
            {
                sp_id = _config.sp_id,
                sp_password = _config.sp_password,
                user_id = userId,
                transaction_id = transactionId,
            };
            return await _sendService.SendRequestAsync<CA2StatusCheckResponse>(HttpMethodType.post, CA2_URI.uriGetResult, request);
        }

        public X509Certificate2 ConvertCertStringToX509(string rawCert)
        {
            byte[] certBytes = Convert.FromBase64String(rawCert);
            X509Certificate2 certificate = new X509Certificate2(certBytes);
            return certificate;
        }
    }
}
