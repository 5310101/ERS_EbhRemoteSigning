using ERS_Domain;
using ERS_Domain.clsUtilities;
using ERS_Domain.Dtos;
using ERS_Domain.Exceptions;
using ERS_Domain.Model;
using IntrustCA_Winservice.Services;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace IntrustCA_Winservice.Process
{
    /// <summary>
    /// process quet db ho so roi day vao queue HoSoChuaKy
    /// </summary>
    public class ScanHoSoProcess
    {
        private readonly IChannel _channel;
        private readonly CoreService _coreService;
        private readonly int NumberHSScan = int.Parse(ConfigurationManager.AppSettings["NumberHSScan"]);

        public ScanHoSoProcess(IChannel channel, CoreService coreService)
        {
            _channel = channel;
            _coreService = coreService;
            //khai bao DLQ de xu ly cac message nack
            //_channel.ExchangeDeclareAsync("ErrorHS.exc", ExchangeType.Direct, true).GetAwaiter().GetResult();
            //_channel.QueueBindAsync("ErrorHS.dlq", "ErrorHS.exc", "ErrorHS.dlq");
            _channel.QueueDeclareAsync(queue: "HSIntrust.dlq", durable: true, exclusive: false, autoDelete: false, arguments: null).GetAwaiter().GetResult();

            //retry queue se retry sau moi 5s
            var retryargs = RabbitMQHelper.CreateQueueArgument("", "HSIntrust.q", true);

            _channel.QueueDeclareAsync(queue: "HSIntrust.retry.q",
                                 durable: true,
                                 exclusive: false,
                                 autoDelete: false,
                                 arguments: retryargs).GetAwaiter().GetResult();

            var dlqargs = RabbitMQHelper.CreateQueueArgument("", "HSIntrust.dlq", false);

            _channel.QueueDeclareAsync(queue: "HSIntrust.q",
                                 durable: true,
                                 exclusive: false,
                                 autoDelete: false,
                                 arguments: dlqargs).GetAwaiter().GetResult();

        }

        public void StartProcess()
        {
            List<string> PublishedList = new List<string>();
            try
            {
                using (var dt = _coreService.GetHS(RemoteSigningProvider.Intrust, TrangThaiHoso.ChuaTaoFile, NumberHSScan))
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
                        _channel.BasicPublishAsync(exchange: "", routingKey: "HSIntrust.q", mandatory: false, basicProperties: properties, body: hs.GetBytesStringFromJsonObject()).GetAwaiter().GetResult();
                        //sau khi publish message thi update database cho hoso
                        PublishedList.Add(guid);
                    }
                }
            }
            catch (Exception ex)
            {
                Utilities.logger.ErrorLog(ex, "Error when publishing message to queue HSIntrust");
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
