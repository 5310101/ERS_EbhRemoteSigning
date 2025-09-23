using IntrustCA_Winservice.Services;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntrustCA_Winservice.Process
{
    public class PublishToKhaiProcess
    {
        private readonly IChannel _channel;
        private readonly CoreService _coreService;

        public PublishToKhaiProcess(IChannel channel, CoreService coreService)
        {
            _channel = channel;
            _coreService = coreService;

            _channel.QueueDeclareAsync(queue: "HSIntrust",
                                 durable: true,
                                 exclusive: false,
                                 autoDelete: false,
                                 arguments: null).GetAwaiter().GetResult();
            _channel.QueueDeclareAsync(queue: "TKIntrust",
                                 durable: true,
                                 exclusive: false,
                                 autoDelete: false,
                                 arguments: null).GetAwaiter().GetResult();

        }

        public void DoWork()
        {
            //1 service 1 lan chi lay ra 3 ho so de xu ly
            _channel.BasicQosAsync(0,3,false).GetAwaiter().GetResult();
            //consume message
            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += (model, ea) =>
            {


                return Task.CompletedTask;
            };
        }
    }
}
