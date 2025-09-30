using RabbitMQ.Client.Events;
using RabbitMQ.Client;
using System.Collections.Generic;
using System.Threading.Tasks;
using ERS_Domain.clsUtilities;
using System.Configuration;

namespace IntrustCA_Winservice
{
    public static class RabbitMQHelper
    {
        public static Dictionary<string, object> CreateQueueArgument(string exchange, string routingKey, bool isRetry = false)
        {
            int ttl = int.Parse(ConfigurationManager.AppSettings["RABBITMQ_TTL"]);
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

        public static async Task HandleError(IChannel channel, BasicDeliverEventArgs ea, int maxTry, string retryRoutingKey)
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
                //qua du lan retry thi nack roi gui den dlq
                await channel.BasicNackAsync(ea.DeliveryTag, false, false);
            }
        }
    }
}
