using ERS_Domain;
using ERS_Domain.clsUtilities;
using ERS_Domain.Dtos;
using ERS_Domain.Exceptions;
using ERS_Domain.Model;
using IntrustCA_Winservice.Services;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace IntrustCA_Winservice.Process
{
    public class PublishToKhaiProcess
    {
        private readonly IChannel _channel;
        private readonly CoreService _coreService;
        private int hsPerProcess = int.Parse(ConfigurationManager.AppSettings["HS_PER_PROCESS"]);

        public PublishToKhaiProcess(IChannel channel, CoreService coreService)
        {
            _channel = channel;
            _coreService = coreService;

            //khai bao DLQ de xu ly cac message nack
            _channel.ExchangeDeclareAsync("ErrorHSExchange", ExchangeType.Direct, true).GetAwaiter().GetResult();
            _channel.QueueDeclareAsync(queue: "ErrorHS", durable: true, exclusive: false, autoDelete: false, arguments: null).GetAwaiter().GetResult();
            _channel.QueueBindAsync("ErrorHS", "ErrorHSExchange", "ErrorHS");

            var args = new Dictionary<string, object>
            {
                { "x-dead-letter-exchange", "ErrorHSExchange" },
                { "x-dead-letter-routing-key", "ErrorHS" }
            };

            _channel.QueueDeclareAsync(queue: "HSIntrust",
                                 durable: true,
                                 exclusive: false,
                                 autoDelete: false,
                                 arguments: args).GetAwaiter().GetResult();
            _channel.QueueDeclareAsync(queue: "TKIntrust",
                                 durable: true,
                                 exclusive: false,
                                 autoDelete: false,
                                 arguments: null).GetAwaiter().GetResult();

        }

        public void DoWork()
        {
            //1 service 1 lan chi lay ra 10 ho so de xu ly
            _channel.BasicQosAsync(0,10,false).GetAwaiter().GetResult();
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
                    HandleError(ea).GetAwaiter().GetResult();
                }
                
                return Task.CompletedTask;
            };
            _channel.BasicConsumeAsync(queue: "HSIntrust",autoAck: false, consumer).GetAwaiter().GetResult();
        }

        public async Task ProcessSendMessage(BasicDeliverEventArgs ea)
        {
            var bytedata = ea.Body.ToArray();
            string jsonMessage = Encoding.UTF8.GetString(bytedata);

            //xu ly message
            var hs = jsonMessage.SerializeJsonTo<HoSoMessage>();
            DataTable dt = _coreService.GetToKhai(hs.guid);
            if (dt.Rows.Count == 0)
            {
                throw new Exception("Cannot find file");
            }
            //voi moi to khai se publish vao queue TKIntrust roi update trang thai to khai
            foreach (DataRow row in dt.Rows)
            {
                var tkPublish = new ToKhaiMessage
                {
                    Id = row["Id"].SafeNumber<int>(),
                    GuidHS = row["GuidHS"].SafeString(),
                    Uid = row["uid"].SafeString(),
                    SerialNumber = row["SerialNumber"].SafeString(),
                    FilePath = row["FilePath"].SafeString(),
                    LoaiFile = (FileType)row["LoaiFile"].SafeNumber<int>(),
                };
                byte[] body = tkPublish.GetBytesStringFromJsonObject();
                var properties = new BasicProperties
                {
                    Persistent = true
                };
                await _channel.BasicPublishAsync(exchange: "", routingKey: "TKIntrust", mandatory: false, basicProperties: properties, body: body);
                //update trang thai tung to khai, loi thi ghi log lai
                try
                {
                    var tkUpdate = new UpdateToKhaiDto
                    {
                        Id = row["Id"].SafeNumber<int>(),
                        TrangThai = TrangThaiFile.DangXuLy
                    };
                    //trong th update loi thi cung ko nguy hiem vi o process nay chi select theo guidhs
                    bool isUpdated = _coreService.UpdateToKhai(tkUpdate);
                    if (isUpdated == false)
                    {
                        throw new DatabaseInteractException("File's status update failed");
                    }
                }
                catch (DatabaseInteractException ex)
                {
                    //loi van cho ack vi day chi la loi khi ko update dc trang thai file trong bang ToKhai_RS
                    Utilities.logger.ErrorLog(ex, "Update database failed");
                }
            }
        }

        private async Task HandleError(BasicDeliverEventArgs ea)
        {
            int errorCount = 0;
            if (ea.BasicProperties.Headers != null && ea.BasicProperties.Headers.ContainsKey("errorCount"))
            {
                errorCount = ea.BasicProperties.Headers["errorCount"].SafeNumber<int>();
            }

            errorCount++;
            //retry 3 lan neu ko thanh cong thi se nack
            if(errorCount < 3)
            {
                var retryProp = new BasicProperties
                {
                    Persistent = true,
                    Headers = new Dictionary<string, object> { ["errorCount"] =  errorCount },
                };

                await _channel.BasicPublishAsync(exchange: "" , routingKey: ea.RoutingKey, mandatory: false,basicProperties: retryProp, body: ea.Body.ToArray());
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
