using CA2_Winservice.Cache;
using CA2_Winservice.Services;
using ERS_Domain;
using ERS_Domain.CAService;
using ERS_Domain.clsUtilities;
using ERS_Domain.CustomSigner.CA2CustomSigner;
using ERS_Domain.Dtos;
using ERS_Domain.Exceptions;
using ERS_Domain.Model;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Linq;
using System.Threading.Tasks;

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

        public SignToKhaiProcess(CoreService coreService, CA2SigningService ca2Service, IChannel chanel)
        {
            _coreService = coreService;
            _ca2Service = ca2Service;
            _channel = chanel;

            _channel.QueueDeclareAsync("HSCA2.ReadyToSign.q", true, false, false,
                RabbitMQHelper.CreateQueueArgument("", "HSCA2.ReadyToSign.dlq", false)).GetAwaiter().GetResult();
            //ko lay dc ket qa thi sau 5s se retry
            _channel.QueueDeclareAsync("HSCA2.ReadyToSign.retry.q", true, false, false,
                RabbitMQHelper.CreateQueueArgument("", "HSCA2.ReadyToSign.q", true, 5)).GetAwaiter().GetResult();
            _channel.QueueDeclareAsync("HSCA2.ReadyToSign.dlq", true, false, false, null).GetAwaiter().GetResult();
        }

        public void DoWork()
        {
            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += async (model, ea) =>
            {
                try
                {
                    await ProcessMessage(ea);
                    await _channel.BasicAckAsync(ea.DeliveryTag, false);
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
            var res = await _ca2Service.GetSignedResult(hs.uid, hs.guid);
            if (res == null || res.status_code != 200)
            {
                throw new Exception("Cannot get result from CA2 server");
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
                        break;
                    case FileType.XML:
                        var profileXML = ProfileCache.GetProfileCache<CA2XMlSignerProfile>(tk.TransactionId);
                        var sigValue1 = lstSign.FirstOrDefault(s => s.doc_id == tk.TransactionId)?.signature_value;
                        if (string.IsNullOrEmpty(sigValue1) || sigValue1.SafeString() == "-2")
                        {
                            throw new CA2ServerSignException($"server cannot sign file {tk.TenToKhai} ,id: {tk.TransactionId}");
                        }
                        CA2SignUtilities.AddSignatureXml(tk.FilePath, profileXML.SignedInfo, sigValue1, profileXML.CertData, DateTime.Now, tk.TenToKhai.GetNodeSignXml());
                        var updateTk1 = new UpdateToKhaiDto
                        {
                            Id = tk.Id,
                            TrangThai = TrangThaiFile.DaKy,
                        };
                        _coreService.UpdateToKhai(updateTk1);
                        break;
                    default:
                        throw new Exception($"Unsupported file type {tk.LoaiFile} in HoSo {hs.guid}");

                }
            }
            //Tao file BHXH dien tu

        }
    }
}
