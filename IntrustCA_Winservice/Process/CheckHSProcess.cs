using ERS_Domain;
using ERS_Domain.Cache;
using ERS_Domain.clsUtilities;
using ERS_Domain.Dtos;
using IntrustCA_Winservice.Services;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace IntrustCA_Winservice.Process
{
    public class CheckProcess
    {
        private readonly IChannel _channel;
        private readonly CoreService _coreService;
        private ushort hsCheckPerProcess = ushort.Parse(ConfigurationManager.AppSettings["HSCHECK_PER_PROCESS"]);

        public CheckProcess(IChannel channel, CoreService coreService)
        {
            _channel = channel;
            _coreService = coreService;
            _channel.QueueDeclareAsync("HSReadyToSign.q",
                        durable: true,
                        exclusive: false,
                        autoDelete: false,
                        arguments: null
                        ).GetAwaiter().GetResult();

            _channel.QueueDeclareAsync("CreateSession.q",
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null
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
                    _channel.BasicAckAsync(ea.DeliveryTag, false).GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    Utilities.logger.ErrorLog(ex, "Process Message failed");
                    //xu ly nack
                    HandleError(ea, 3).GetAwaiter().GetResult();
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
            var hs = jsonMessage.SerializeJsonTo<HoSoMessage>();
            //ky tung to khai
            if(hs == null || hs.toKhais.Any() == false)
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

        private async Task HandleError(BasicDeliverEventArgs ea, int maxTry)
        {
            int errorCount = 0;
            if (ea.BasicProperties.Headers != null && ea.BasicProperties.Headers.ContainsKey("errorCount"))
            {
                errorCount = ea.BasicProperties.Headers["errorCount"].SafeNumber<int>();
            }

            errorCount++;
            //retry 3 lan neu ko thanh cong thi se nack
            if(errorCount < maxTry)
            {
                var retryProp = new BasicProperties
                {
                    Persistent = true,
                    Headers = new Dictionary<string, object> { ["errorCount"] =  errorCount },
                };

                await _channel.BasicPublishAsync(exchange: "" , routingKey: "HS.retry.q", mandatory: false,basicProperties: retryProp, body: ea.Body.ToArray());
                await _channel.BasicAckAsync(ea.DeliveryTag, false);
            }
            else
            {
                //qua 3 lan thi nack roi gui den dlq
                await _channel.BasicNackAsync(ea.DeliveryTag, false, false);
            }
        }
    }
}
