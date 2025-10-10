using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO.Pipes;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;

namespace IntrustCA_Winservice
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
