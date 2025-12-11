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
using System.IO;
using System.Threading.Tasks;

namespace CA2_Winservice.Process
{
    public class SignHSBHXHProcess
    {
        private readonly CoreService _coreService;
        private readonly CA2SigningService _ca2Service;
        private readonly IChannel _channel;

        public SignHSBHXHProcess(IChannel channel, CoreService coreService, CA2SigningService ca2Service)
        {
            _coreService = coreService;
            _ca2Service = ca2Service;
            _channel = channel;
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
                catch(NotSigningFromUserException ex)
                {
                    await RabbitMQHelper.RetryMessage(_channel, ea, 30, "HSCA2.ReadyToSign.GetResult.retry.q", ex, _coreService.UpdateHS);
                }
                catch (Exception ex)
                {
                    Utilities.logger.ErrorLog(ex, "Consume message error", "SignHSBHXHProcess");
                    await RabbitMQHelper.HandleError(_channel, ea, 3, "HSCA2.ReadyToSign.retry.q", ex, _coreService.UpdateHS);
                }
            };
            _channel.BasicConsumeAsync("HSCA2.ReadyToSign.q", false, consumer).GetAwaiter().GetResult();    
        }   

        private async Task ProcessMessage(BasicDeliverEventArgs ea)
        {
            var hs = ea.ProcessMessageToObject<HoSoMessage>();  
            if(hs == null)
            {
                throw new Exception("Deserialize error or incorrect message");
            }
            //Lay ket qua ky tu server
            var res = await _ca2Service.GetSignedResult(hs.uid, hs.guid);
            if(res == null || res?.status_code != 200)
            {
                throw new Exception("Cannot get signature value from server");
            }
            //ho so thi chi co 1 file HS
            if (res.data.signatures[0].signature_value == "")
            {
                throw new NotSigningFromUserException("User is not signing");
            }

            var profile = ProfileCache.GetProfileCache<CA2XMlSignerProfile>(hs.guid);
            if (profile == null)
            {
                throw new Exception("Cannot get signing profile from cache");
            }
            //Them cks vao file
            //luon lay gia tri cuoi cung
            string signatureValue = res.data.signatures[res.data.signatures.Length -1].signature_value;
            string nodeKy =Path.GetFileNameWithoutExtension(hs.filePathHS).GetNodeSignXml();
            CA2SignUtilities.AddSignatureXml(hs.filePathHS, profile.SignedInfo, signatureValue, profile.CertData, DateTime.Now, nodeKy);
            //ky xong xoa profile va update trang thai hs
            var hsUpdate = new UpdateHoSoDto
            {
                ListId = new string[] {hs.guid},
                TrangThai = TrangThaiHoso.DaKy,
                FilePath = hs.filePathHS
            };  
            ProfileCache.RemoveProfile(hs.guid);

        }
    }
}
