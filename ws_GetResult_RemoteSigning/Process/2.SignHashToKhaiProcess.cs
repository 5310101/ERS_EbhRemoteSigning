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
    public class SignHashToKhaiProcess : RabbitMqWorkerBase
    {
        private readonly CoreService _coreService;
        private readonly SigningService _signService;
        private static readonly int _retryTtlMs = int.Parse(ConfigurationManager.AppSettings["GETRESULT_RETRY_INTERVAL"]);
        private static readonly int _maxCount = int.Parse(ConfigurationManager.AppSettings["GETRESULT_RETRY_MAXCOUNT"]);

        private static readonly ushort _signTK_HSPrefetch = ushort.Parse(ConfigurationManager.AppSettings["SIGNHASH_TKHS_PREFETCH"]);
        private static readonly ushort _signTK_ConcurrentConsumer = ushort.Parse(ConfigurationManager.AppSettings["SIGNHASH_TKHS_ConcurrentConsumer"]);
        private readonly static int _signhashTKMaxTry = int.Parse(ConfigurationManager.AppSettings["TKSIGNHASH_RETRYMAXTRY"]);

        public SignHashToKhaiProcess(RabbitmqManager manager, CoreService coreService, SigningService signService) : base(manager, _signTK_ConcurrentConsumer,
             new List<RabbitQueueOptions>
            {
                new RabbitQueueOptions
                {
                    MainQueue = "SmartCA.GetResultToKhai.q",
                    RetryQueue = "SmartCA.GetResultToKhai.retry.q",
                    DeadLetterQueue = "SmartCA.GetResultToKhai.dlq",
                    RetryTtlMs = _retryTtlMs,
                    MaxRetryCount = _maxCount,
                },
            }, new List<RabbitMqConsumerOptions>
            {
                new RabbitMqConsumerOptions
                {
                    QueueName = "SmartCA.SignhashToKhai.q",
                    RetryQueue = "SmartCA.SignhashToKhai.retry.q",
                    DeadLetterQueue = "SmartCA.SignhashToKhai.dlq",
                    MaxRetryCount = _signhashTKMaxTry,
                    PrefetchCount = _signTK_HSPrefetch,
                },
            })
        {
            _coreService = coreService;
            _signService = signService;
        }

        //process nay se consume message tu queue SmartCA.SignhashToKhai.q, xu ly ky hash roi push vao queue SmartCA.GetResultToKhai.q
        protected override async Task ProcessMessageAsync(IChannel channel, HoSoMessage hs, CancellationToken cancellationToken, BasicDeliverEventArgs ea )
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            try
            {
                _signService.SignToKhai_VNPT(hs);
                //ky thanh cong roi push vao queue SmartCA.GetResultToKhai.q
                await PublishToAnotherQueue(channel, "", "SmartCA.GetResultToKhai.q", hs.GetBytesStringFromJsonObject()).ConfigureAwait(false);
            }
            catch (FileErrorException ex)
            {
                //neu file bi loi thi ack luon ko ky lai nua, update trang thai to khai va ho so loi
                Utilities.logger.ErrorLog(ex, $"Lỗi đọc hsfile {ex.FilePath}");
                //update trang thai to khai va ho so loi
                _coreService.UpdateToKhai(new UpdateToKhaiDto
                {
                    Id = ex.IdToKhai,
                    TrangThai = TrangThaiFile.KyLoi,
                    ErrMsg = ex.Message,
                });
                _coreService.UpdateHS(new UpdateHoSoDto
                {
                    ListId = new string[] { hs.guid },
                    TrangThai = TrangThaiHoso.KyLoi,
                    ErrMsg = $"Lỗi đọc hsfile {ex.FilePath}"
                });
                //ko nhay vao day thi se handle exception nhu binh thuong, retry 3 lan roi push vao dlq
                //Xoa thu muc chua file bi loi
                _coreService.DeleteTempFolder(Path.GetDirectoryName(ex.FilePath));
            }
        }
    }
}
