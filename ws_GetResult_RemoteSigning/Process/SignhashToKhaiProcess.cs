using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ws_GetResult_RemoteSigning.Utils;

namespace ws_GetResult_RemoteSigning.Process
{
    public class SignhashToKhaiProcess : RabbitMqWorkerBase
    {
        private readonly CoreService _coreService;
        private readonly IChannel _channel;

        public SignhashToKhaiProcess(CoreService coreService, IChannel channel) : base(channel, new List<RabbitQueueOptions>
        {
            new RabbitQueueOptions
            {
                MainQueue = "SmartCA.GetresultToKhai.q",
                RetryQueue = "SmartCA.GetresultToKhai.retry.q",
                DeadLetterQueue = "SmartCA.GetresultToKhai.dlq",
                RetryTtlMs = 5000,
                MaxRetryCount = 3,
                PrefetchCount = 10
            }
        })
        {
            _coreService = coreService;
            _channel = channel;
        }

        protected override Task ProcessMessageAsync(BasicDeliverEventArgs ea, CancellationToken cancellationToken)
        {
            try
            {

            }
            catch (System.Exception)
            {

                throw;
            }
        }
    }
}
