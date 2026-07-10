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
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ws_GetResult_RemoteSigning.Utils;

namespace ws_GetResult_RemoteSigning.Process
{
    public class GetResultTokhaiProcess : RabbitMqWorkerBase
    {
        private readonly CoreService _coreService;  
        private readonly SigningService _signService;
        private static readonly int _retryTtlMs = int.Parse(ConfigurationManager.AppSettings["GETRESULT_RETRY_INTERVAL"]);
        private static readonly int _maxCount = int.Parse(ConfigurationManager.AppSettings["GETRESULT_RETRY_MAXCOUNT"]);
        private static readonly ushort _getResultTKPrefetch = ushort.Parse(ConfigurationManager.AppSettings["GETRESULT_TOKHAI_PREFETCH"]);
        private static readonly int _getResultTKConcurrentConsumer = int.Parse(ConfigurationManager.AppSettings["GETRESULT_TOKHAI_ConcurrentConsumer"]);
        private static readonly string SignedTempFolder = ConfigurationManager.AppSettings["HOSO_TEMP_FOLDER"];

        public GetResultTokhaiProcess(RabbitmqManager manager, CoreService coreService, SigningService signService) : base(manager, _getResultTKConcurrentConsumer,
            new List<RabbitQueueOptions>
            {
                new RabbitQueueOptions
                {
                    MainQueue = "SmartCA.GetResultHoSo.q",
                    RetryQueue = "SmartCA.GetResultHoSo.retry.q",
                    DeadLetterQueue = "SmartCA.GetResultHoSo.dlq",
                    RetryTtlMs = _retryTtlMs,
                    MaxRetryCount = _maxCount,
                },
            },
            new List<RabbitMqConsumerOptions>
            {
                new RabbitMqConsumerOptions
                {
                    QueueName = "SmartCA.GetResultToKhai.q",
                    RetryQueue = "SmartCA.GetResultToKhai.retry.q",
                    DeadLetterQueue = "SmartCA.GetResultToKhai.dlq",
                    MaxRetryCount = _maxCount,
                    PrefetchCount = _getResultTKPrefetch,
                },
            })
        {
            _coreService = coreService;
            _signService = signService;
        }

        //process nay se consume message tu queue SmartCA.GetResultToKhai.q,
        //xu ly lay ket qua ky to khai roi them chu ky so push vao queue SmartCA.SignHashHoSo.q
        protected override async Task ProcessMessageAsync(IChannel channel,HoSoMessage hs, CancellationToken cancellationToken, BasicDeliverEventArgs ea)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            try
            {
                _signService.GetResultToKhai_VNPT(hs);
            }
            //handle trong truong hop nguoi ky chua ky file 
            catch (NotSigningFromUserException)
            {
                // retry nhieu lan hon, ko tang count retry, de cho nguoi ky co the ky file roi moi retry
                byte[] body = hs.GetBytesStringFromJsonObject();
                int retryCount = GetRetryCount(ea.BasicProperties);
                //retry 30 lan, moi lan retry cach nhau 8s, neu qua 30 lan (150s) thi push vao dlq
                await PublishToRetryQueueAsync(channel, "SmartCA.GetResultToKhai.retry.q", "SmartCA.GetResultToKhai.dlq", body, retryCount, 30).ConfigureAwait(false);
                return;
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
                return;
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
                return;
            }

            //ok thi tao file xml ho so
            //process nay se signhash HSNghiepVu va HSDKLD
            string pathSaveHS = Path.Combine(SignedTempFolder, $"{hs.guid}");
            _signService.CreateBHXHDienTu(hs, pathSaveHS);
            if (hs.typeDK == TypeHS.HSNV)
            {
                _signService.CreateBHXHDienTu(hs, pathSaveHS);
            }
            else if (hs.typeDK == TypeHS.HSDKLanDau)
            {
                string pathFile = Path.Combine(pathSaveHS, $"{hs.maNV}.xml");
                var dtHSDK = _coreService.GetHSDKLanDau(hs.guid);   
               _signService.CreateFileHoSoDK_LanDau(hs, pathFile, dtHSDK); 
            }
            //signhash  
            try
            {
                _signService.SignHoSoBHXH(hs);
                //push vao queue SmartCA.SignHashHoSo.q de lay ket qua ky tu CA
                await PublishToAnotherQueue(channel,"", "SmartCA.GetResultHoSo.q", hs.GetBytesStringFromJsonObject()).ConfigureAwait(false);
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
                //ko nhay vao day thi se handle exception nhu binh thuong, retry 3 lan roi push vao dlq
                //Xoa thu muc chua file bi loi
                _coreService.DeleteTempFolder(Path.GetDirectoryName(ex.FilePath));
            }
        }
    }
}
