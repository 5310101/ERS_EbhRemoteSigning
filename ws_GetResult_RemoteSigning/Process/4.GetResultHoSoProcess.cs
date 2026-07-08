using ERS_Domain.clsUtilities;
using ERS_Domain.Dtos;
using ERS_Domain.Exceptions;
using ERS_Domain.Model;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ws_GetResult_RemoteSigning.Utils;

namespace ws_GetResult_RemoteSigning.Process
{
    //process nay se consume message tu 2 queue SmartCA.GetResultHoSo.q va SmartCA.GetResultHSDK.q
    public class GetResultHoSoProcess : RabbitMqWorkerBase
    {
        private readonly CoreService _coreService;
        private readonly SigningService _signService;

        public GetResultHoSoProcess(IChannel channel, CoreService coreService, SigningService signService) : base(channel, null,
            new List<RabbitMqConsumerOptions>
            {
                new RabbitMqConsumerOptions
                {
                    QueueName = "SmartCA.GetResultHoSo.q",
                    PrefetchCount = 5,
                },
            })
        {
            _coreService = coreService;
            _signService = signService;
        }

        protected override async Task ProcessMessageAsync(BasicDeliverEventArgs ea, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            var hs = ProcessMessageToObject<HoSoMessage>(ea);
            if (hs == null)
            {
                throw new Exception("Deserialize error or incorrect message");
            }
            try
            {
                _signService.GetResultHoSo_VNPT(hs);
            }
            catch (NotSigningFromUserException)
            {
                // retry nhieu lan hon, ko tang count retry, de cho nguoi ky co the ky file roi moi retry
                byte[] body = hs.GetBytesStringFromJsonObject();
                int retryCount = GetRetryCount(ea.BasicProperties);
                //retry 30 lan, moi lan retry cach nhau 8s, neu qua 30 lan (240s) thi push vao dlq
                await PublishToRetryQueueAsync("SmartCA.GetResultHoSo.retry.q", "SmartCA.GetResultHoSo.dlq", body, retryCount, 30);
            }
            // het han hoac tu choi ky thi ack luon, update trang thai hoso, ko retry nua
            catch (SigningExpiredException ex)
            {
                _coreService.UpdateHS(new UpdateHoSoDto
                {
                    ListId = new string[] { hs.guid },
                    TrangThai = TrangThaiHoso.HetHan,
                    ErrMsg = ex.Message,
                });
                _coreService.DeleteTempFolder(Path.GetDirectoryName(ex.FilePath));
            }
            catch (SigningRejectedException ex)
            {
                _coreService.UpdateHS(new UpdateHoSoDto
                {
                    ListId = new string[] { hs.guid },
                    TrangThai = TrangThaiHoso.KyLoi,
                    ErrMsg = ex.Message,
                });
                _coreService.DeleteTempFolder(Path.GetDirectoryName(ex.FilePath));
            }
        }
    }
}
