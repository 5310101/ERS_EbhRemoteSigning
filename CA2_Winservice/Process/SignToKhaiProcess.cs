using CA2_Winservice.Cache;
using CA2_Winservice.Services;
using ERS_Domain;
using ERS_Domain.CAService;
using ERS_Domain.clsUtilities;
using ERS_Domain.CustomSigner.CA2CustomSigner;
using ERS_Domain.Dtos;
using ERS_Domain.Exceptions;
using ERS_Domain.Model;
using ERS_Domain.Request;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Xml;

namespace CA2_Winservice.Process
{
    /// <summary>
    /// Process lay signature data tu server gan vao file to khai va tao file xml BHXHDienTu
    /// </summary>
    public class SignToKhaiProcess
    {
        private readonly CoreService _coreService;
        private readonly CA2SigningService _ca2Service;
        private readonly IChannel _channel;

        public SignToKhaiProcess(IChannel chanel, CoreService coreService, CA2SigningService ca2Service)
        {
            _coreService = coreService;
            _ca2Service = ca2Service;
            _channel = chanel;
        }

        public void StartProcess()
        {
            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += async (model, ea) =>
            {
                try
                {
                    await ProcessMessage(ea);
                    await _channel.BasicAckAsync(ea.DeliveryTag, false);
                }
                catch (NotSigningFromUserException ex)
                {
                    //push lai vao queue de retry de thoi gian 5s va ko tang lan retry
                    await RabbitMQHelper.RetryMessage(_channel, ea, 30, "HSCA2.ToKhai.GetResult.retry.q", ex, _coreService.UpdateHS);

                }
                catch (CA2ServerSignException ex)
                {
                    Utilities.logger.ErrorLog(ex, "The server failed to sign the data, but still returned HTTP 200", "SignToKhaiProcess");
                    //trong th server ky loi thi ko retry nua cap nhat ho so loi luon
                    await RabbitMQHelper.HandleError(_channel, ea, -1, "HSCA2.ToKhai.retry.q", ex, _coreService.UpdateHS);
                }
                catch (Exception ex)
                {
                    Utilities.logger.ErrorLog(ex, "Consume message error", "SignToKhaiProcess");
                    await RabbitMQHelper.HandleError(_channel, ea, 3, "HSCA2.ToKhai.retry.q", ex, _coreService.UpdateHS);
                }
            };
            _channel.BasicConsumeAsync("HSCA2.ToKhai.q", false, consumer).GetAwaiter().GetResult();
        }

        private async Task ProcessMessage(BasicDeliverEventArgs ea)
        {
            //lay ket qua ky tu server
            var hs = ea.ProcessMessageToObject<HoSoMessage>();
            if (hs == null)
            {
                throw new Exception("Deserialize error or incorrect message");
            }
            var res = await _ca2Service.GetSignedResult(hs.uid, hs.transactionId);
            if (res == null || res.status_code != 200 )
            {
                throw new Exception("Cannot get result from CA2 server");
            }
            //Cho du chu ky tu server ms ky
            if (res.data.signatures.Any(s => s.signature_value == ""))
            {
                throw new NotSigningFromUserException("Waiting user to sign");
            }
            //lay profile ky
            var lstSign = res.data.signatures;
            foreach (ToKhai tk in hs.toKhais)
            {
                switch (tk.LoaiFile)
                {
                    case FileType.PDF:
                        var profilePDF = ProfileCache.GetProfileCache<CA2PDFSignProfile>(tk.TransactionId);
                        var sigValue = lstSign.FirstOrDefault(s => s.doc_id == tk.TransactionId)?.signature_value;
                        if (string.IsNullOrEmpty(sigValue) || sigValue.SafeString() == "-2")
                        {
                            throw new CA2ServerSignException($"server cannot sign file {tk.TenToKhai} ,id: {tk.TransactionId}");
                        }
                        CA2SignUtilities.AddSignaturePdf(profilePDF, tk.FilePath, sigValue);
                        //update trang thai tk da ky
                        var updateTk = new UpdateToKhaiDto
                        {
                            Id = tk.Id,
                            TrangThai = TrangThaiFile.DaKy,
                        };
                        _coreService.UpdateToKhai(updateTk);
                        // xoa profile khoi cache
                        ProfileCache.RemoveProfile(tk.TransactionId);
                        break;
                    case FileType.XML:
                        var profileXML = ProfileCache.GetProfileCache<CA2XMlSignerProfile>(tk.TransactionId);
                        var sigValue1 = lstSign.FirstOrDefault(s => s.doc_id == tk.TransactionId)?.signature_value;
                        if (string.IsNullOrEmpty(sigValue1) || sigValue1.SafeString() == "-2")
                        {
                            throw new CA2ServerSignException($"server cannot sign file {tk.TenToKhai} ,id: {tk.TransactionId}");
                        }
                        CA2SignUtilities.AddSignatureXml(tk.FilePath, profileXML.SignedInfo, sigValue1, profileXML.CertData, DateTime.Now, Path.GetFileNameWithoutExtension(tk.TenToKhai).GetNodeSignXml());
                        var updateTk1 = new UpdateToKhaiDto
                        {
                            Id = tk.Id,
                            TrangThai = TrangThaiFile.DaKy,
                        };
                        _coreService.UpdateToKhai(updateTk1);
                        //xoa profile khoi cache
                        ProfileCache.RemoveProfile(tk.TransactionId);   
                        break;
                    default:
                        throw new Exception($"Unsupported file type {tk.LoaiFile} in HoSo {hs.guid}");

                }
            }
            //Tao file BHXH dien tu
            string filePathHS;
            switch (hs.typeDK)
            {
                case TypeHS.HSNV:
                    filePathHS = Path.Combine(Utilities.globalPath.SignedTempFolder, hs.guid, "BHXHDienTu.xml");
                    hs.CreateFileBHXHDienTu(filePathHS);
                    break;
                case TypeHS.HSDKLanDau:
                    filePathHS = Path.Combine(Utilities.globalPath.SignedTempFolder, hs.guid,$"{hs.maNV}.xml");
                    DataTable dtHSDK = _coreService.GetHSDKLanDau(hs.guid);
                    hs.CreateFileHoSoDK_LanDau(filePathHS, dtHSDK);
                    break;
                default:
                    throw new Exception($"{hs.typeDK} is not supported");
            }

            var res1 = await _ca2Service.GetCertificates(hs.uid, "EBH".GenGuidStr(), hs.serialNumber);
            if (res1 == null || res1?.status_code != 200)
            {
                throw new Exception("Cannot get certificate from CA2 service");
            }
            var cert = res1.data.user_certificates[0];
            //ky hash
            //tao 1 transactionId khac de ky ho so
            hs.transactionId = "HS".GenGuidStr();
            XmlElement signedInfo = CA2SignUtilities.CreateSignedInfoNode(filePathHS, "");
            string hash_to_sign_xml = CA2SignUtilities.CreateHashXmlToSign(signedInfo);
            CA2XMlSignerProfile profile = new CA2XMlSignerProfile
            {
                DocId = hs.transactionId,
                SignedInfo = signedInfo,    
                CertData = cert.cert_data,
            };
            //them profile vao profile cache
            ProfileCache.SetProfileCache(profile.DocId, profile);
            FileToSign fts = new FileToSign
            {
                data_to_be_signed = hash_to_sign_xml,
                doc_id = profile.DocId,
                file_type = "xml",
            };
            var res3 = await _ca2Service.SignHashValue(hs.uid, hs.transactionId, new FileToSign[] {fts},hs.serialNumber, DateTime.Now);
            if(res3 == null || res3?.status_code != 200)
            {
                throw new Exception("File cannot be signed hash by the server");
            }
            //neu ky thanh cong thi update trang thai ho so
            var hsUpdateDto = new UpdateHoSoDto
            {
                ListId = new string[] { hs.guid },
                TrangThai = TrangThaiHoso.DaKyHash,
                FilePath = filePathHS,
            };
            _coreService.UpdateHS(hsUpdateDto);
            hs.filePathHS = filePathHS;
            //publish sang queue readyToSign
            await _channel.BasicPublishAsync("", "HSCA2.ReadyToSign.q", false, new BasicProperties
            {
               DeliveryMode = DeliveryModes.Persistent
            }, hs.GetBytesStringFromJsonObject());
        }
    }
}
