using ERS_Domain.clsUtilities;
using ERS_Domain.Dtos;
using ERS_Domain.Exceptions;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ws_GetResult_RemoteSigning.Utils;

namespace ws_GetResult_RemoteSigning.Process
{
    public class SignHashToKhaiProcess : RabbitMqWorkerBase
    {
        private readonly IChannel _channel;
        private readonly SigningService _signService;

        public SignHashToKhaiProcess(IChannel channel, CoreService coreService, SigningService signService) : base(channel,
             new List<RabbitQueueOptions>
            {
                new RabbitQueueOptions
                {
                    MainQueue = "SmartCA.GetResultToKhai.q",
                    RetryQueue = "SmartCA.GetResultToKhai.retry.q",
                    DeadLetterQueue = "SmartCA.GetResultToKhai.dlq",
                    RetryTtlMs = 5000,
                    MaxRetryCount = 3,
                },
            }, new List<RabbitMqConsumerOptions>
            {
                new RabbitMqConsumerOptions
                {
                    QueueName = "SmartCA.SignhashToKhai.q",
                    //ky 5 ho so 1 luc 
                    PrefetchCount = 5,
                },
            })
        {
            _channel = channel;
            _signService = signService;
        }

        //process nay se consume message tu queue SmartCA.SignhashToKhai.q, xu ly ky hash roi push vao queue SmartCA.GetResultToKhai.q
        protected override async Task ProcessMessageAsync(BasicDeliverEventArgs ea, CancellationToken cancellationToken)
        {
            var hs = ProcessMessageToObject<HoSoMessage>(ea);
            if (hs == null)
            {
                throw new Exception("Deserialize error or incorrect message");
            }
            try
            {
                _signService.SignToKhai_VNPT(hs);
            }
            catch (FileErrorException ex)
            {
                //neu file bi loi thi push vao queue SmartCA.GetResultToKhai.q de thong bao loi
                throw;
            }
            
            //ky thanh cong roi push vao queue SmartCA.GetResultToKhai.q
            var properties = new BasicProperties()
            {
                Persistent = true
            };
            await _channel.BasicPublishAsync(exchange: "", routingKey: "SmartCA.GetResultToKhai.q", mandatory: false, basicProperties: properties, body: hs.GetBytesStringFromJsonObject()).ConfigureAwait(false);
        }
    }
}
