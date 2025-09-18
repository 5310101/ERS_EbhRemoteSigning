using ERS_Domain.clsUtilities;
using RabbitMQ.Client;

namespace IntrustCA_Winservice.Process
{
    /// <summary>
    /// process quet db ho so roi day vao queue HoSoChuaKy
    /// </summary>
    public class ScanHoSoProcess
    {
        private readonly IChannel _channel;
        private readonly DbService _dbService;

        public ScanHoSoProcess(IChannel channel)
        {
            _channel = channel;
            _channel.QueueDeclareAsync(queue: "HoSoChuaKy",
                                 durable: true,
                                 exclusive: false,
                                 autoDelete: false,
                                 arguments: null).GetAwaiter().GetResult();
        }

        public void Dowork()
        {
            
        }
    }
}
