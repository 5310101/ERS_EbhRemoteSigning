using RabbitMQ.Client;
using System;
using System.Configuration;

namespace CA2_Winservice
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
            _conFactory = new ConnectionFactory() { Uri = new Uri(RabbitmqUri) };
            _connection = _conFactory.CreateConnectionAsync().GetAwaiter().GetResult();
        }

        public IChannel CreateChanel() => _connection.CreateChannelAsync().GetAwaiter().GetResult();

        public void Dispose()
        {
            _connection.Dispose();
        }
    }
}
