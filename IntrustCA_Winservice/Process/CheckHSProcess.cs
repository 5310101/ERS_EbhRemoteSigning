using ERS_Domain;
using ERS_Domain.clsUtilities;
using ERS_Domain.Dtos;
using IntrustCA_Domain.Cache;
using IntrustCA_Winservice.Services;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Configuration;
using System.Text;
using System.Threading.Tasks;

namespace IntrustCA_Winservice.Process
{
    public class CheckHSProcess
    {
        private readonly IChannel _channel;
        private readonly CoreService _coreService;
        private ushort hsCheckPerProcess = ushort.Parse(ConfigurationManager.AppSettings["HSCHECK_PER_PROCESS"]);

        public CheckHSProcess(IChannel channel, CoreService coreService)
        {
            _channel = channel;
            _coreService = coreService;
            //declare queue
            var retryprops1 = RabbitMQHelper.CreateQueueArgument("", "HSReadyToSign.q", true);
            _channel.QueueDeclareAsync(queue: "HSReadyToSign.retry.q",
                                 durable: true,
                                 exclusive: false,
                                 autoDelete: false,
                                 arguments: retryprops1).GetAwaiter().GetResult();
            
            _channel.QueueDeclareAsync(queue: "HSReadyToSign.dlq",
                                 durable: true,
                                 exclusive: false,
                                 autoDelete: false,
                                 arguments: null).GetAwaiter().GetResult();
            var dlqProps1 = RabbitMQHelper.CreateQueueArgument("", "HSReadyToSign.dlq", false);
            _channel.QueueDeclareAsync("HSReadyToSign.q",
                        durable: true,
                        exclusive: false,
                        autoDelete: false,
                        arguments: dlqProps1
                        ).GetAwaiter().GetResult();

            var retryprops2 = RabbitMQHelper.CreateQueueArgument("", "CreateSession.q", true);
            _channel.QueueDeclareAsync(queue: "CreateSession.retry.q",
                                 durable: true,
                                 exclusive: false,
                                 autoDelete: false,
                                 arguments: retryprops2).GetAwaiter().GetResult();
            
            _channel.QueueDeclareAsync(queue: "CreateSession.dlq",
                                 durable: true,
                                 exclusive: false,
                                 autoDelete: false,
                                 arguments: null).GetAwaiter().GetResult();
            var dlqProps2 = RabbitMQHelper.CreateQueueArgument("", "CreateSession.dlq", false);
            _channel.QueueDeclareAsync("CreateSession.q",
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: dlqProps2
                ).GetAwaiter().GetResult();
        }
        public void DoWork()
        {
            //1 service 1 lan chi lay ra 10 ho so de xu ly
            _channel.BasicQosAsync(0, hsCheckPerProcess, false).GetAwaiter().GetResult();
            //consume message
            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += (model, ea) =>
            {
                try
                {
                    ProcessSendMessage(ea).GetAwaiter().GetResult();
                    //manual ack message khi xu ly xong
                    _channel.BasicAckAsync(ea.DeliveryTag, false).GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    Utilities.logger.ErrorLog(ex, "Process Message failed");
                    //xu ly nack
                    RabbitMQHelper.HandleError(_channel,ea, 3, "HSIntrust.retry.q").GetAwaiter().GetResult();
                }
                return Task.CompletedTask;
            };
            _channel.BasicConsumeAsync(queue: "HSIntrust.q",autoAck: false, consumer).GetAwaiter().GetResult();
        }

        public async Task ProcessSendMessage(BasicDeliverEventArgs ea)
        {
            var bytedata = ea.Body.ToArray();
            string jsonMessage = Encoding.UTF8.GetString(bytedata);

            //xu ly message
            var hs = jsonMessage.DeserializeJsonTo<HoSoMessage>();
            if(hs == null)
            {
                throw new Exception("Serialization failed or no file was found");
            }
            //check uid da tao phien ky chua neu da tao phien ky se chuyen sang queue HSReadyToSign.q de ky 
            //Neu chua co phien ky hay phien ky da het han thi chuyen sang queue CreateSession.q
            if(SessionCache.ExistStore(hs.uid) == false)
            {
                var props1 = new BasicProperties
                {
                    Persistent = true,
                };
                await _channel.BasicPublishAsync(exchange: "", routingKey: "CreateSession.q", mandatory: false, basicProperties: props1, body: bytedata);
                
                return;
            }
            var props2 = new BasicProperties
            {
                Persistent = true,
            };
            await _channel.BasicPublishAsync(exchange: "", routingKey: "HSReadyToSign.q", mandatory: false, basicProperties: props2, body: bytedata);
        }
    }
}
