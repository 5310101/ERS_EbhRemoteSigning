using ERS_Domain;
using ERS_Domain.Dtos;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;

namespace IntrustCA_Winservice.Consumer
{
    public class DLQConsumer
    {
        private readonly IChannel _channel;
        private readonly Func<string, bool> HandleMessage;
        private readonly string _queueName;
        private readonly ushort _numberMessagePerProcess;

        public DLQConsumer(IChannel channel, Func<string, bool> handleMessage, string queueName, ushort numberMessagePerProcess)
        {
            _channel = channel;
            HandleMessage = handleMessage;
            _queueName = queueName;
            _numberMessagePerProcess = numberMessagePerProcess;
        }

        public void ConsumeMessage()
        {
            try
            {
                _channel.BasicQosAsync(0, _numberMessagePerProcess, false);
                var consumer = new AsyncEventingBasicConsumer(_channel);
                consumer.ReceivedAsync += async (model, ea) =>
                {
                    try
                    {
                        //ghi log message loi ra file
                        var hs = ea.ProcessMessageToObject<HoSoMessage>();
                        //xu ly

                        await _channel.BasicAckAsync(ea.DeliveryTag, false);
                    }
                    catch (Exception ex)
                    {
                        //tam thoi chi ghi log 
                        Utilities.logger.ErrorLog(ex, "Consume dead letter message error", "HandleErrorProcess");
                    }
                };
                _channel.BasicConsumeAsync("DeadLetter.dlq", false, consumer).GetAwaiter().GetResult();

            }
            catch (Exception)
            {

                throw;
            }

        }
    }
}
