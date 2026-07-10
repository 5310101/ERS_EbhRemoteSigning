using ERS_Domain.clsUtilities;
using ERS_Domain.Dtos;
using ERS_Domain.Exceptions;
using ERS_Domain.Model;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
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

        private static readonly ushort _getResultTKPrefetch = ushort.Parse(ConfigurationManager.AppSettings["GETRESULT_HOSO_PREFETCH"]);
        private static readonly int _getResultTKConcurrentConsumer = int.Parse(ConfigurationManager.AppSettings["GETRESULT_HOSO_ConcurrentConsumer"]);
        private static readonly int _maxCount = int.Parse(ConfigurationManager.AppSettings["GETRESULT_RETRY_MAXCOUNT"]);

        public GetResultHoSoProcess(RabbitmqManager manager, CoreService coreService, SigningService signService) : base(manager, _getResultTKConcurrentConsumer, null,
            new List<RabbitMqConsumerOptions>
            {
                new RabbitMqConsumerOptions
                {
                    QueueName = "SmartCA.GetResultHoSo.q",
                     RetryQueue = "SmartCA.GetResultHoSo.retry.q",
                    DeadLetterQueue = "SmartCA.GetResultHoSo.dlq",
                    MaxRetryCount = _maxCount,
                    PrefetchCount = _getResultTKPrefetch,
                },
            })
        {
            _coreService = coreService;
            _signService = signService;
        }

        protected override async Task ProcessMessageAsync(IChannel channel, HoSoMessage hs, CancellationToken cancellationToken, BasicDeliverEventArgs ea)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
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
                await PublishToRetryQueueAsync(channel, "SmartCA.GetResultHoSo.retry.q", "SmartCA.GetResultHoSo.dlq", body, retryCount, 30).ConfigureAwait(false);
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
