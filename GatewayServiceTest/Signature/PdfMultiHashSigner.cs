using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VnptHashSignatures.Interface;
using VnptHashSignatures.Pdf;

namespace GatewayServiceTest.Signature
{
    public class PdfMultiHashSigner
    {
        private Dictionary<string, IHashSigner> _signers;
        private static readonly log4net.ILog Logger = log4net.LogManager.GetLogger(typeof(PdfMultiHashSigner));
        
        public PdfMultiHashSigner(List<byte[]> datas, string certBase64)
        {
            _signers = new Dictionary<string, IHashSigner>();
            for(var i = 0; i < datas.Count; i++)
            {
                var signer = _getSecondHashValue(datas[i], certBase64);
                if (signer == null)
                {
                    Logger.Warn($"Failed to init HashSigner for entry {i}");
                    continue;
                }
                var secondHash = signer.GetSecondHashAsBase64();
                _signers.Add(secondHash, signer);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public byte[] GetHashValue()
        {
            try
            {
                List<HashEntity> entities = new List<HashEntity>();
                for (int i = 0; i < _signers.Count; i++)
                {
                    entities.Add(new HashEntity
                    {
                        Data = _signers.ElementAt(i).Key
                    });
                }
                var data = JsonConvert.SerializeObject(entities);
                return Encoding.GetEncoding("UTF-8").GetBytes(data);
            }
            catch(Exception ex)
            {
                Logger.Warn($"Exception in GetHashValue: {ex.Message}");
            }
            return null;
        }

        public List<byte[]> Sign(string externalSigned)
        {
            List<byte[]> signeds = new List<byte[]>();
            List<HashSigned> signatures = null;
            try
            {
                string jsonStr = Encoding.UTF8.GetString(Convert.FromBase64String(externalSigned));
                signatures = JsonConvert.DeserializeObject<List<HashSigned>>(jsonStr);
            }
            catch(Exception ex)
            {
                Logger.Warn($"External signauters invalid. {ex.Message}");
            }
            
            for(var i = 0; i < signatures.Count; i++)
            {
                var signature = signatures[i];
                if (signature == null)
                {
                    continue;
                }

                // 4.1. Kiểm tra ký thành công hay không
                if (signature.Code != 0)
                {
                    Logger.Error("Siggning error for " + signature.Data);
                    continue;
                }

                // 4.3. Đóng gói chữ ký vào file gốc
                try
                {
                    if (!_signers.TryGetValue(signature.Data, out IHashSigner signer))
                    {
                        Logger.Error("Not valid data " + signature.Data);
                        continue;
                    }
                    var x = Encoding.UTF8.GetString(Convert.FromBase64String(signature.Signature));
                    if (!signer.CheckHashSignature(signature.Signature))
                    {
                        Logger.Error("Signature not match for " + signature.Data);
                        return null;
                    }
                    byte[] signed = signer.Sign(signature.Signature);
                    signeds.Add(signed);
                }
                catch(Exception ex)
                {
                    Logger.Error($"Package signed data failed for {signature.Data}. {ex.Message}");
                }
            }
            return signeds;
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

        private IHashSigner _getSecondHashValue(byte[] unsignData, string certBase64)
        {
            IHashSigner signer = HashSignerFactory.GenerateSigner(unsignData, certBase64, HashSignerFactory.PDF);

            #region Optional -----------------------------------
            // Signature reason
            ((PdfHashSigner)signer).SetReason("Sign reason");
            // Signature custom image
            var image = "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";
            ((PdfHashSigner)signer).SetCustomImage(Convert.FromBase64String(image));
            // Signing page
            ((PdfHashSigner)signer).SetSigningPage(1);
            ((PdfHashSigner)signer).SetSignaturePosition(50, 50, 250, 100);
            #endregion -----------------------------------------

            return signer;
        }
    }
}
