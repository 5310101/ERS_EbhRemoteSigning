using ERS_Domain;
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
    public class SignHashHSDKProcess : RabbitMqWorkerBase
    {
        private readonly CoreService _coreService;
        private readonly SigningService _signService;
        private readonly static ushort _signHSDKPrefetch = ushort.Parse(ConfigurationManager.AppSettings["HSDK_PREFETCH"]);
        private readonly static int signHSDK_ConcurrentConsumer = ushort.Parse(ConfigurationManager.AppSettings["SIGNHSDK_ConcurrentConsumer"]);
        private readonly static int _signhashHSDKMaxTry = int.Parse(ConfigurationManager.AppSettings["HSDKSIGNHASH_RETRYMAXTRY"]);


        public SignHashHSDKProcess(RabbitmqManager manager, CoreService coreService, SigningService signService) : base(manager, signHSDK_ConcurrentConsumer, null,
            new List<RabbitMqConsumerOptions>
            {
                new RabbitMqConsumerOptions
                {
                    QueueName = "SmartCA.SignhashHSDK.q",
                    MaxRetryCount = _signhashHSDKMaxTry,
                    RetryQueue = "SmartCA.SignhashHSDK.retry.q",
                    DeadLetterQueue = "SmartCA.SignhashHSDK.dlq",
                    PrefetchCount = _signHSDKPrefetch,
                },
            })
        {
            _coreService = coreService;
            _signService = signService;
        }

        protected override async Task ProcessMessageAsync(IChannel channel, HoSoMessage hs, CancellationToken cancellationToken, BasicDeliverEventArgs ea )
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            //voi truong hop to khai dang ky thì ky thang file
           
            string pathFileHSDK = Path.Combine(Utilities.globalPath.SignedTempFolder, hs.guid, $"{hs.maNV}.xml");
            hs.filePathHS = pathFileHSDK;
            //signhash
            try
            {
                _signService.SignHoSoBHXH(hs);
                //push message to queue get result
                await PublishToAnotherQueue(channel, "", "SmartCA.GetResultHSDK.q", hs.GetBytesStringFromJsonObject()).ConfigureAwait(false);
            }
            catch (FileErrorException ex)
            {
                //neu file bi loi thi ack luon ko ky lai nua, update trang thai to khai va ho so loi
                Utilities.logger.ErrorLog(ex, $"Lỗi đọc hsfile {ex.FilePath}");
                _coreService.UpdateHS(new UpdateHoSoDto
                {
                    ListId = new string[] { hs.guid },
                    TrangThai = TrangThaiHoso.KyLoi,
                    ErrMsg = $"Lỗi đọc hsfile {ex.FilePath}"
                });
                _coreService.DeleteTempFolder(Path.GetDirectoryName(ex.FilePath));
            }
        }
    }
}
