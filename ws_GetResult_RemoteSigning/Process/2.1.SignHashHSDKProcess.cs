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
        //biến quy đinh 1 lần timer sẽ xử lý bao nhiêu file HSDK (trừ hồ sơ đăng ký lấy mã đơn vị lần đầu)
        private readonly static ushort _signHSDKCount = ushort.Parse(ConfigurationManager.AppSettings["HSDK_COUNT"]);

        public SignHashHSDKProcess(IChannel chanel, CoreService coreService, SigningService signService) : base(chanel, null,
            new List<RabbitMqConsumerOptions>
            {
                new RabbitMqConsumerOptions
                {
                    QueueName = "SmartCA.SignhashHSDK.q",
                    PrefetchCount = _signHSDKCount,
                },
            })
        {
            _coreService = coreService;
            _signService = signService;
        }

        protected override async Task ProcessMessageAsync(BasicDeliverEventArgs ea, CancellationToken cancellationToken)
        {
            if(cancellationToken.IsCancellationRequested)
            {
                return;
            }
            //voi truong hop to khai dang ky thì ky thang file
            var hs = ProcessMessageToObject<HoSoMessage>(ea);   
            if (hs == null)
            {
                throw new Exception("Deserialize error or incorrect message");
            }
            string pathFileHSDK = Path.Combine(Utilities.globalPath.SignedTempFolder, hs.guid, $"{hs.maNV}.xml");
            hs.filePathHS = pathFileHSDK;
            //signhash
            try
            {
                _signService.SignHoSoBHXH(hs);
                //push message to queue get result
                await PublishToAnotherQueue("", "SmartCA.GetResultHSDK.q", hs.GetBytesStringFromJsonObject());
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
