using System;
using System.Linq;
using System.Threading.Tasks;
using ERS_Domain;
using ERS_Domain.Cache;
using ERS_Domain.clsUtilities;
using IntrustCA_Domain.Dtos;
using IntrustCA_Domain;
using IntrustCA_Winservice.Services;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace IntrustCA_Winservice.Process
{
    public class SignHSProcess
    {
        private readonly IChannel _channel;
        private readonly CoreService _coreService;

        private readonly ushort numHSSignPerProcess = ushort.Parse(System.Configuration.ConfigurationManager.AppSettings["NUMBERHSSIGN_PERPROCESS"]);

        public SignHSProcess(IChannel channel, CoreService coreService)
        {
            _channel = channel;
            _coreService = coreService;
        }

        public void DoWork()
        {
            _channel.BasicQosAsync(0, numHSSignPerProcess, false);
            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += async (model, ea) =>
            {
                try
                {
                    await ProcessMessage(ea);
                    await _channel.BasicAckAsync(ea.DeliveryTag, false);
                }
                catch (Exception ex)
                {
                    Utilities.logger.ErrorLog(ex, "Consume message error", "SignHSProcess");
                    await RabbitMQHelper.HandleError(_channel, ea, 3, "HSReadyToSign.retry.q");
                }
            };
            _channel.BasicConsumeAsync("HSReadyToSign.q", false, consumer).GetAwaiter().GetResult();

        }

        private async Task ProcessMessage(BasicDeliverEventArgs ea)
        {
            string jsonMessage = System.Text.Encoding.UTF8.GetString(ea.Body.ToArray());
            var hs = jsonMessage.DeserializeJsonTo<HoSoMessage>();
            if(hs == null || hs.toKhais.Any() == false)
            {
                throw new Exception("Deserialize error or incorrect message");
            }
            //Tao Service ky
            ICACertificate cert = IntrustRemoteSigningService.GetCertificate(hs.uid, hs.serialNumber);
            if (cert == null) throw new Exception("Không tìm thấy chữ ký số");
            //luu y: khi tao service nay thi chac chan hs can ky da duoc tao store trong cache vi chi o process SignHS moi khoi tao nen o day se get chu ko set nua
            SignSessionStore store = SessionCache.GetOrSetStore(hs.uid, cert);
            if(store == null) throw new Exception("Không tạo được phiên ký");
            var signService = new IntrustRemoteSigningService(store);
            //ky
        }
    }
}
