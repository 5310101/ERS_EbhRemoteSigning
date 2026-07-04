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
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ws_GetResult_RemoteSigning.Utils;

namespace ws_GetResult_RemoteSigning.BackgroudWorker
{
    public class ScanHoSoProcess : RabbitMqWorkerBase
    {
        private readonly CoreService _coreService;
        private readonly IChannel _channel;
        private readonly int NumberHSScan = int.Parse(ConfigurationManager.AppSettings["HOSO_COUNT"]);
        public ScanHoSoProcess(IChannel channel, CoreService coreService) : base(channel, new List<RabbitQueueOptions>
        {
            new RabbitQueueOptions
            {
                MainQueue = "SmartCA.SignhashToKhai.q",
                RetryQueue = "SmartCA.SignhashToKhai.retry.q",
                DeadLetterQueue = "SmartCA.SignhashToKhai.dlq",
                RetryTtlMs = 5000,
                MaxRetryCount = 3,
            },
            new RabbitQueueOptions
            {
                MainQueue = "SmartCA.SignhashHSDK.q",
                RetryQueue = "SmartCA.SignhashHSDK.retry.q",
                DeadLetterQueue = "SmartCA.SignhashHSDK.dlq",
                RetryTtlMs = 5000,
                MaxRetryCount = 3,
            },
        })
        {
            _coreService = coreService;
            _channel = channel;
        }

        /// <summary>
        /// scan ho so chua ky va gui len queue ky to khai doi voi ho so nghiep vu va ho so dang ky lan dau, 
        /// ho so dang ky thi gui len queue HSDKSignHash
        /// </summary>
        /// <param name="ea"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected override async Task ProcessMessageAsync(BasicDeliverEventArgs ea, CancellationToken cancellationToken)
        {
            List<string> PublishedList = new List<string>();
            try
            {
                //lay ho so chua ky tu db
                using (var dt = _coreService.GetHS(RemoteSigningProvider.VNPT, TrangThaiHoso.ChuaTaoFile, NumberHSScan))
                {
                    if (dt == null || dt?.Rows.Count == 0) return;
                    //List<Task> tasks = new List<Task>();
                    foreach (DataRow row in dt.Rows)
                    {
                        string guid = row["Guid"].SafeString();
                        var hs = new HoSoMessage
                        {
                            guid = guid,
                            uid = row["uid"].SafeString(),
                            serialNumber = row["SerialNumber"].SafeString(),
                            typeDK = (TypeHS)row["typeDK"].SafeNumber<int>(),
                            MST = row["FromMST"].SafeString(),
                            MDV = row["FromMDV"].SafeString(),
                            tenDV = row["TenDonVi"].SafeString(),
                            loaiDoiTuong = row["LoaiDoiTuong"].SafeNumber<int>(),
                            nguoiKy = row["NguoiKy"].SafeString(),
                            dienThoai = row["DienThoai"].SafeString(),
                            maCQBHXH = row["MaCQBH"].SafeString(),
                            tenHS = row["TenHS"].SafeString(),
                            maNV = row["MaNV"].SafeString()
                        };
                        using (var dtToKhai = _coreService.GetToKhai(guid))
                        {
                            //hs dang ky se ko co file to khai
                            if (dtToKhai.AsEnumerable().Any() == true)
                            {
                                List<ToKhai> listTokhai = new List<ToKhai>();
                                foreach (DataRow rowTK in dtToKhai.Rows)
                                {
                                    var tkPublish = new ToKhai
                                    {
                                        Id = rowTK["id"].SafeNumber<int>(),
                                        TenToKhai = rowTK["TenToKhai"].SafeString(),
                                        MoTaToKhai = rowTK["MoTa"].SafeString(),
                                        GuidHS = rowTK["GuidHS"].SafeString(),
                                        FilePath = rowTK["FilePath"].SafeString(),
                                        LoaiFile = (FileType)rowTK["LoaiFile"].SafeNumber<int>(),
                                    };
                                    listTokhai.Add(tkPublish);

                                    try
                                    {
                                        var tkUpdate = new UpdateToKhaiDto
                                        {
                                            Id = rowTK["id"].SafeNumber<int>(),
                                            TrangThai = TrangThaiFile.DangXuLy
                                        };
                                        //trong th update loi thi van cho chay tiep
                                        bool isUpdated = _coreService.UpdateToKhai(tkUpdate);
                                        if (isUpdated == false)
                                        {
                                            throw new DatabaseInteractException("File's status update failed");
                                        }
                                    }
                                    catch (DatabaseInteractException ex)
                                    {
                                        //loi van cho chay tiep vi day chi la loi khi ko update dc trang thai file trong bang ToKhai_RS
                                        Utilities.logger.ErrorLog(ex, $"Update database failed: ToKhai: {row["id"].ToString()}");
                                    }
                                }
                                hs.toKhais = listTokhai.ToArray();
                            }
                        }

                        var properties = new BasicProperties()
                        {
                            Persistent = true
                        };
                        if (hs.typeDK == TypeHS.HSNV || hs.typeDK == TypeHS.HSDKLanDau)
                        {
                            await _channel.BasicPublishAsync(exchange: "", routingKey: "SmartCA.SignhashToKhai.q", mandatory: false, basicProperties: properties, body: hs.GetBytesStringFromJsonObject()).ConfigureAwait(false);
                        }
                        else
                        {
                            await _channel.BasicPublishAsync(exchange: "", routingKey: "SmartCA.SignhashHSDK.q", mandatory: false, basicProperties: properties, body: hs.GetBytesStringFromJsonObject()).ConfigureAwait(false);
                        }
                        //sau khi publish message thi update database cho hoso
                        PublishedList.Add(guid);
                    }
                }
            }
            catch (Exception ex)
            {
                Utilities.logger.ErrorLog(ex, "Error when publishing message to queue HSCA2");
                return;
            }
            UpdateHoSoDto updateHS = new UpdateHoSoDto()
            {
                ListId = PublishedList.ToArray(),
                TrangThai = TrangThaiHoso.DangXuLy
            };
            //update co trans de ko bi mat trang thai message
            //neu update loi thi dung luon service luu lai cac Guid HS update loi vi co the da duoc publish vao rabit mq
            bool isUpdate = _coreService.UpdateHS(updateHS);
            if (isUpdate == false)
            {
                throw new DatabaseInteractException("Update database failed", PublishedList.ToArray());
            }
        }
    }
}