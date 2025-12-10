using RabbitMQ.Client.Events;
using RabbitMQ.Client;
using System.Collections.Generic;
using System.Threading.Tasks;
using ERS_Domain.clsUtilities;
using System.Configuration;
using System;
using ERS_Domain.Dtos;
using System.Text;
using ERS_Domain.Model;

namespace CA2_Winservice
{
    public delegate bool UpdateHoSoLoi(UpdateHoSoDto updateHSDto);
    public static class RabbitMQHelper
    {
        public static Dictionary<string, object> CreateQueueArgument(string exchange, string routingKey, bool isRetry = false, int ttl = 0)
        {
            if (ttl == 0) { ttl = int.Parse(ConfigurationManager.AppSettings["RABBITMQ_TTL"]); };
            if (!isRetry)
            {
                return new Dictionary<string, object>
                {
                    { "x-dead-letter-exchange", exchange },
                    { "x-dead-letter-routing-key", routingKey }
                };
            }
            return new Dictionary<string, object>
            {
                { "x-message-ttl", ttl },
                { "x-dead-letter-exchange", exchange },
                { "x-dead-letter-routing-key", routingKey }
            };
        }

        public static async Task HandleError(IChannel channel, BasicDeliverEventArgs ea, int maxTry, string retryRoutingKey, Exception ex, UpdateHoSoLoi updateHSLoi)
        {
            int errorCount = 0;
            if (ea.BasicProperties.Headers != null && ea.BasicProperties.Headers.ContainsKey("errorCount"))
            {
                errorCount = ea.BasicProperties.Headers["errorCount"].SafeNumber<int>();
            }

            errorCount++;
            //retry 1 so lan neu ko thanh cong thi se nack
            if (errorCount < maxTry)
            {
                var retryProp = new BasicProperties
                {
                    Persistent = true,
                    Headers = new Dictionary<string, object> { ["errorCount"] = errorCount },
                };

                await channel.BasicPublishAsync(exchange: "", routingKey: retryRoutingKey, mandatory: false, basicProperties: retryProp, body: ea.Body.ToArray());
                await channel.BasicAckAsync(ea.DeliveryTag, false);
            }
            else
            {
                //qua du lan retry thi nack roi gui den dlq, update trang thai hoso loi
                var hs = ea.ProcessMessageToObject<HoSoMessage>();
                if (hs != null && updateHSLoi != null)
                {
                    UpdateHoSoDto updateHSDto = new UpdateHoSoDto
                    {
                        ListId = new string[] { hs.guid },
                        TrangThai = TrangThaiHoso.KyLoi,
                        ErrMsg = ex.Message 
                    };
                    updateHSLoi(updateHSDto);
                }
                await channel.BasicNackAsync(ea.DeliveryTag, false, false);
            }
        }

        public static T ProcessMessageToObject<T>(this BasicDeliverEventArgs ea)
        {
            var bytedata = ea.Body.ToArray();
            string jsonMessage = Encoding.UTF8.GetString(bytedata);

            //xu ly message
            return jsonMessage.DeserializeJsonTo<T>();
        }
    }
}
