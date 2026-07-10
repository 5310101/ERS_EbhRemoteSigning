using RabbitMQ.Client;
using System;
using System.Configuration;
using System.Threading.Tasks;

namespace ws_GetResult_RemoteSigning
{
    public class RabbitmqManager : IDisposable
    {
		private string _rabbitmqUri;
		public string RabbitmqUri
        {
			get 
			{
				if (string.IsNullOrEmpty(_rabbitmqUri))
                {
                    _rabbitmqUri = ConfigurationManager.AppSettings["RMQ_CONNECTION_URI"];
                }   
                return _rabbitmqUri; 
			}
		}

		private readonly IConnection _connection;
		private readonly ConnectionFactory _conFactory;

        public RabbitmqManager()
        {
            _conFactory = new ConnectionFactory()
            { 
                Uri = new Uri(RabbitmqUri),
                AutomaticRecoveryEnabled = false 
            };
            if(_connection != null && _connection.IsOpen)
            {
                _connection.CloseAsync().GetAwaiter().GetResult();
                _connection.Dispose();
            }
            _connection = _conFactory.CreateConnectionAsync().GetAwaiter().GetResult();
        }

        public IChannel CreateChannel() => _connection.CreateChannelAsync().GetAwaiter().GetResult();
        public async Task<IChannel> CreateChannelAsync() => await _connection.CreateChannelAsync();

        public void Dispose()
        {
            _connection.Dispose();
        }
    }
}
