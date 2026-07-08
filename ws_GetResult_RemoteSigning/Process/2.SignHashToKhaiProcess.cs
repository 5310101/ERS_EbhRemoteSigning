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

        //biến quy định 1 lần timer tick sẽ xử lý bao nhiêu ho so mặc định là 3 ho so chua to khai
        private static readonly ushort _signTK_HSCount = ushort.Parse(ConfigurationManager.AppSettings["TKHS_COUNT"]);

        public SignHashToKhaiProcess(IChannel channel, CoreService coreService, SigningService signService) : base(channel,
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
                    //ky n ho so 1 luc 
                    PrefetchCount = _signTK_HSCount,
                },
            })
        {
            _coreService = coreService;
            _signService = signService;
        }

        //process nay se consume message tu queue SmartCA.SignhashToKhai.q, xu ly ky hash roi push vao queue SmartCA.GetResultToKhai.q
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
                _signService.SignToKhai_VNPT(hs);
                //ky thanh cong roi push vao queue SmartCA.GetResultToKhai.q
                await PublishToAnotherQueue("", "SmartCA.GetResultToKhai.q", hs.GetBytesStringFromJsonObject()).ConfigureAwait(false);
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
