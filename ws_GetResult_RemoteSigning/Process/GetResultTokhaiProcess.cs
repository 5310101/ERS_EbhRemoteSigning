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
        private readonly string SignedTempFolder = ConfigurationManager.AppSettings["HOSO_TEMP_FOLDER"];

        public GetResultTokhaiProcess(IChannel channel, CoreService coreService, SigningService signService) : base(channel, 
            new List<RabbitQueueOptions>
            {
                new RabbitQueueOptions
                {
                    MainQueue = "SmartCA.SignHashHoSo.q",
                    RetryQueue = "SmartCA.SignHashHoSo.retry.q",
                    DeadLetterQueue = "SmartCA.SignHashHoSo.dlq",
                    RetryTtlMs = _retryTtlMs,
                    MaxRetryCount = _maxCount,
                },
            },
            new List<RabbitMqConsumerOptions>
            {
                new RabbitMqConsumerOptions
                {
                    QueueName = "SmartCA.GetResultToKhai.q",
                    PrefetchCount = 5,
                },
            })
        {
            _coreService = coreService;
            _signService = signService;
        }

        //process nay se consume message tu queue SmartCA.GetResultToKhai.q,
        //xu ly lay ket qua ky to khai roi them chu ky so push vao queue SmartCA.SignHashHoSo.q
        protected override async Task ProcessMessageAsync(BasicDeliverEventArgs ea, CancellationToken cancellationToken)
        {
            var hs = ProcessMessageToObject<HoSoMessage>(ea);
            if (hs == null)
            {
                throw new Exception("Deserialize error or incorrect message");
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
                await PublishToRetryQueueAsync( "SmartCA.GetResultToKhai.retry.q", "SmartCA.GetResultToKhai.dlq", body, retryCount, 30);
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
            }
            catch (SigningRejectedException ex)
            {
                _coreService.UpdateHS(new UpdateHoSoDto
                {
                    ListId = new string[] { hs.guid },
                    TrangThai = TrangThaiHoso.KyLoi,
                    ErrMsg = ex.Message,
                }); 
            }

            //ok thi tao file xml ho so
            string pathSaveHS = Path.Combine(SignedTempFolder, $"{hs.guid}");
            _signService.CreateBHXHDienTu(hs, pathSaveHS);

            //signhash ho so


            //push vao queue SmartCA.SignHashHoSo.q de ky hash ho so
            await PublishToAnotherQueue("","SmartCA.SignHashHoSo.q", hs.GetBytesStringFromJsonObject());
        }
    }
}
