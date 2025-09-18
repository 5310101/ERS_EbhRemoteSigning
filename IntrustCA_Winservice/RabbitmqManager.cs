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
		private string _hostName;
		public string HostName
		{
			get 
			{
				if (string.IsNullOrEmpty(_hostName))
                {
                    _hostName = ConfigurationManager.AppSettings["RMQ_HOSTNAME"];
                }   
                return _hostName; 
			}
		}

		private readonly IConnection _connection;
		private readonly ConnectionFactory _conFactory;

        public RabbitmqManager()
        {
            _conFactory = new ConnectionFactory() { HostName = HostName };
            _connection = _conFactory.CreateConnectionAsync().GetAwaiter().GetResult();
        }

        public IChannel CreateChanel() => _connection.CreateChannelAsync().GetAwaiter().GetResult();

        public void Dispose()
        {
            _connection.Dispose();
        }
    }
}
