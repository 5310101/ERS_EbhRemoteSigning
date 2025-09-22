using ERS_Domain.clsUtilities;
using ERS_Domain.Dtos;
using IntrustderCA_Domain.Dtos;
using RabbitMQ.Client;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

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
            _dbService = new DbService();
            _channel.QueueDeclareAsync(queue: "HSIntrust",
                                 durable: true,
                                 exclusive: false,
                                 autoDelete: false,
                                 arguments: null).GetAwaiter().GetResult();
        }

        public void Dowork()
        {
            var dt = _dbService.GetDataTable("SELECT uid,Guid,SerialNumber,typeDK FROM HoSo_RS WHERE CAProvider = 2 AND TrangThai = 4 ORDER BY NgayGui DESC");
            if (dt == null || dt.AsEnumerable().Any() == false) return;
            List<string> PublishedList = new List<string>();
            foreach(DataRow row in dt.AsEnumerable())
            {
                string guid = row["Guid"].SafeString();
                var hs = new HoSoMessage{ guid = guid, uid = row["uid"].SafeString(), serialNumber = row["SerialNumber"].SafeString(), typeDK = row["typeDK"].SafeNumber<int>() }; 
                _channel.BasicPublishAsync(exchange: "", routingKey: "",body: hs.GetBytesString()).GetAwaiter().GetResult();
                //sau khi publish message thi update database cho hoso
                PublishedList.Add(guid);
            }
            _dbService.ExecQuery("UPDATE HoSo_RS SET TrangThai=@TrangThai, LastGet=@LastGet WHERE Guid IN ")
        }
    }
}
