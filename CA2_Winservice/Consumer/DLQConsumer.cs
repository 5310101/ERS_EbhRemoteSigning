using ERS_Domain;
using ERS_Domain.Dtos;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;

namespace CA2_Winservice.Consumer
{
    public class DLQConsumer
    {
        private readonly IChannel _channel;
        private readonly Func<HoSoMessage, string, bool> _handleMessage;
        private readonly string _queueName;
        private readonly ushort _numberMessagePerProcess;

        public DLQConsumer(IChannel channel, Func<HoSoMessage, string, bool> handleMessage, string queueName, ushort numberMessagePerProcess)
        {
            _channel = channel;
            _handleMessage = handleMessage;
            _queueName = queueName;
            _numberMessagePerProcess = numberMessagePerProcess;
        }

        public void ConsumeMessage()
        {
            _channel.BasicQosAsync(0, _numberMessagePerProcess, false).GetAwaiter().GetResult();
            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += async (model, ea) =>
            {
                try
                {
                    //ghi log message loi ra file
                    var hs = ea.ProcessMessageToObject<HoSoMessage>();
                    //xu ly mesage
                    bool? isSuccess = _handleMessage?.Invoke(hs, _queueName);
                    if (isSuccess.HasValue && isSuccess == false) throw new Exception("Handle dead letter message error");
                    await _channel.BasicAckAsync(ea.DeliveryTag, false);
                }
                catch (Exception ex)
                {
                    //tam thoi chi ghi log 
                    Utilities.logger.ErrorLog(ex, "Consume dead letter message error", "HandleErrorProcess");
                }
            };
            _channel.BasicConsumeAsync(_queueName, false, consumer).GetAwaiter().GetResult();

        }
    }
}
