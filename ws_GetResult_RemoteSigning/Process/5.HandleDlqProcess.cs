using ERS_Domain;
using ERS_Domain.Dtos;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ws_GetResult_RemoteSigning.Utils;

namespace ws_GetResult_RemoteSigning.Process
{
    /// <summary>
    /// Process xu ly cac message trong queue dlq, ko retry lai, update trang thai xoa thu muc
    /// log ra de phan tich sau nay
    /// </summary>
    public class HandleDlqProcess : RabbitMqWorkerBase
    {
        private static readonly string[] AllDlqs = { "SmartCA.SignhashToKhai.dlq",
                                                     "SmartCA.SignhashHSDK.dlq" ,
                                                     "SmartCA.GetResultToKhai.dlq",
                                                     "SmartCA.GetResultHoSo.dlq" };
        private readonly string SignedTempFolder = ConfigurationManager.AppSettings["HOSO_TEMP_FOLDER"];
        private readonly CoreService _coreService;

        public HandleDlqProcess(IChannel chanel, CoreService coreService) : base(chanel, null,
            AllDlqs.Select(dlq => new RabbitMqConsumerOptions
            {
                QueueName = dlq,
                PrefetchCount = 5,
            }).ToList()
            )
        {
            _coreService = coreService;
        }

        protected override async Task ProcessMessageAsync(BasicDeliverEventArgs ea, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            //log ra cac message loi 
            var hs = ProcessMessageToObject<HoSoMessage>(ea);
            if (hs == null)
            {
                Utilities.logger.ErrorLog($"[HandleDlqProcess] Message in dlq is not valid json: {ea.Body}", "Serialize Error");
            }

            //log ra cac message loi 
            Utilities.logger.ErrorLog($"[HandleDlqProcess] Message in dlq: {hs.guid}", "DLQ Message");
            //xoa thu muc lien quan den message nay
            string pathHS = Path.Combine(SignedTempFolder, hs.guid);
            _coreService.DeleteTempFolder(pathHS);
            _coreService.UpdateHS(new UpdateHoSoDto
            {
                ListId = new string[] { hs.guid },
                TrangThai = ERS_Domain.Model.TrangThaiHoso.KyLoi,
                ErrMsg = "Retry more than 3 times"
            });
            await Task.CompletedTask;
        }
    }
}
