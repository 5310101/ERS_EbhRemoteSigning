using ERS_Domain;
using ERS_Domain.Dtos;
using IntrustCA_Domain.Cache;
using IntrustCA_Domain;
using IntrustCA_Domain.Dtos;
using IntrustCA_Winservice.Services;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Threading.Tasks;

namespace IntrustCA_Winservice.Process
{
    public class CreateSessionStoreProcess
    {
        private readonly IChannel _channel;
        private readonly CoreService _coreService;

        //so message xu ly song song, dung chung cho prefetch va ConsumerDispatchConcurrency cua channel
        public static readonly ushort NumberOfSession = ushort.Parse(System.Configuration.ConfigurationManager.AppSettings["SESSIONCREATE_PERPROCESS"]);
        public CreateSessionStoreProcess(IChannel channel, CoreService coreService)
        {
            _channel = channel;
            _coreService = coreService;
        }

        public void StartProcess()
        {
            _channel.BasicQosAsync(0, NumberOfSession, false).GetAwaiter().GetResult();
            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += async (model, ea) =>
            {
                try
                {
                    await ProcessMessage(ea);
                    await _channel.BasicAckAsync(ea.DeliveryTag, false);
                }
                catch (SessionRejectedException ex)
                {
                    //user tu choi/ko xac nhan kip => bao loi ho so luon, ko retry de tranh hoi lai user
                    Utilities.logger.ErrorLog(ex, "Create session rejected", "CreateSessionStoreProcess");
                    await RabbitMQHelper.HandleError(_channel, ea, 1, "CreateSession.retry.q", ex, _coreService.UpdateHS);
                }
                catch (Exception ex)
                {
                    Utilities.logger.ErrorLog(ex, "Consume message error", "CreateSessionStoreProcess");
                    await RabbitMQHelper.HandleError(_channel, ea, 3, "CreateSession.retry.q", ex, _coreService.UpdateHS);
                }
            };
            _channel.BasicConsumeAsync("CreateSession.q", false, consumer).GetAwaiter().GetResult();
        }

        private async Task ProcessMessage(BasicDeliverEventArgs ea)
        {
            var hs = ea.ProcessMessageToObject<HoSoMessage>();
            if (hs == null)
            {
                throw new Exception("Deserialize error or incorrect message");
            }
            //lay cert
            var cert = IntrustRSHelper.GetCertificate(hs.uid, hs.serialNumber);
            await SessionCache.GetOrSetStoreAsync(hs.uid, cert);

            //day message ve queue HoSo san sang ky
            var props = new BasicProperties
            {
                Persistent = true
            };
            await _channel.BasicPublishAsync(exchange: "" , routingKey: "HSReadyToSign.q", mandatory: false, basicProperties: props, ea.Body.ToArray());
        }
    }
}
