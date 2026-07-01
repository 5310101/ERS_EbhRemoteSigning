using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ws_GetResult_RemoteSigning.BackgroudWorker
{
    public class ScanHoSoProcess : RabbitMqWorkerBase
    {
        public ScanHoSoProcess(IChannel channel, RabbitQueueOptions options, Action<Exception, string> logError = null) : base(channel, options, logError)
        {
            
        }

        protected override Task ProcessMessageAsync(BasicDeliverEventArgs ea, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}