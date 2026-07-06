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
    public class GetResultTokhaiProcess : RabbitMqWorkerBase
    {
        private readonly IChannel _channel;
        private readonly SigningService _signService;

        public GetResultTokhaiProcess(IChannel channel, SigningService signService) : base(channel, 
            new List<RabbitQueueOptions>
            {
                new RabbitQueueOptions
                {
                    MainQueue = "SmartCA.SignHashHoSo.q",
                    RetryQueue = "SmartCA.SignHashHoSo.retry.q",
                    DeadLetterQueue = "SmartCA.SignHashHoSo.dlq",
                    RetryTtlMs = 5000,
                    MaxRetryCount = 3,
                },
            },
            new List<RabbitMqConsumerOptions>
            {
                new RabbitMqConsumerOptions
                {
                    QueueName = "SmartCA.GetResultToKhai.q",
                    PrefetchCount = 5,
                },
            })
        {
            _channel = channel;
            _signService = signService;
        }

        //process nay se consume message tu queue SmartCA.GetResultToKhai.q,
        //xu ly lay ket qua ky to khai roi them chu ky so push vao queue SmartCA.SignHashHoSo.q
        protected override Task ProcessMessageAsync(BasicDeliverEventArgs ea, CancellationToken cancellationToken)
        {
            var hs = ProcessMessageToObject<HoSoMessage>(ea);
            if (hs == null)
            {
                throw new Exception("Deserialize error or incorrect message");
            }
            try
            {
                _signService.GetResultToKhai_VNPT(hs);
            }
            //handle trong truong hop nguoi ky chua ky file 
            catch (NotSigningFromUserException)
            {
                // retry nhieu lan hon, ko tang count retry, de cho nguoi ky co the ky file roi moi retry
                

            }

        }
    }
}
