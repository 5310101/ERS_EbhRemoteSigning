using System;
using System.Linq;
using System.Threading.Tasks;
using ERS_Domain;
using ERS_Domain.Cache;
using ERS_Domain.clsUtilities;
using IntrustCA_Domain.Dtos;
using IntrustCA_Domain;
using IntrustCA_Winservice.Services;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using IntrustderCA_Domain.Dtos;
using System.Collections.Generic;
using ERS_Domain.Dtos;
using ERS_Domain.Model;
using System.IO;

namespace IntrustCA_Winservice.Process
{
    public class SignHSProcess
    {
        private readonly IChannel _channel;
        private readonly CoreService _coreService;

        private readonly ushort numHSSignPerProcess = ushort.Parse(System.Configuration.ConfigurationManager.AppSettings["NUMBERHSSIGN_PERPROCESS"]);

        public SignHSProcess(IChannel channel, CoreService coreService)
        {
            _channel = channel;
            _coreService = coreService;
        }

        public void DoWork()
        {
            _channel.BasicQosAsync(0, numHSSignPerProcess, false);
            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += async (model, ea) =>
            {
                try
                {
                    await ProcessMessage(ea);
                    await _channel.BasicAckAsync(ea.DeliveryTag, false);
                }
                catch (Exception ex)
                {
                    Utilities.logger.ErrorLog(ex, "Consume message error", "SignHSProcess");
                    await RabbitMQHelper.HandleError(_channel, ea, 3, "HSReadyToSign.retry.q");
                }
            };
            _channel.BasicConsumeAsync("HSReadyToSign.q", false, consumer).GetAwaiter().GetResult();

        }

        private async Task ProcessMessage(BasicDeliverEventArgs ea)
        {
            string jsonMessage = System.Text.Encoding.UTF8.GetString(ea.Body.ToArray());
            var hs = jsonMessage.DeserializeJsonTo<HoSoMessage>();
            if(hs == null || hs.toKhais.Any() == false)
            {
                throw new Exception("Deserialize error or incorrect message");
            }
            //Tao Service ky
            ICACertificate cert = IntrustRSHelper.GetCertificate(hs.uid, hs.serialNumber);
            if (cert == null) throw new Exception("Không tìm thấy chữ ký số");
            //luu y: khi tao service nay thi chac chan hs can ky da duoc tao store trong cache vi chi o process SignHS moi khoi tao nen o day se get chu ko set nua
            SignSessionStore store = SessionCache.GetOrSetStore(hs.uid, cert);
            if(store == null) throw new Exception("Không tạo được phiên ký");
            var signService = new IntrustRemoteSigningService(store);
            //ky
            List<FileToSignDto<FileProperties>> listFiles = new List<FileToSignDto<FileProperties>>();
            foreach(ToKhai tk in hs.toKhais)
            {
                FileToSignDto<FileProperties> file = new FileToSignDto<FileProperties>();
                file.file_id = Guid.NewGuid().ToString();
                file.file_name = Path.GetFileName(tk.FilePath );
                file.extension = Path.GetExtension(tk.FilePath).TrimStart('.').ToLower();
                file.content_file = Convert.ToBase64String(File.ReadAllBytes(tk.FilePath));
                //tao fileproperty cho tung file
                if (tk.LoaiFile == FileType.PDF)
                {
                    file.properties = IntrustRSHelper.CreatePropertiesDefault(file.file_name) as PdfProperties;
                }
                else if (tk.LoaiFile == FileType.XML)
                {
                    file.properties = IntrustRSHelper.CreatePropertiesDefault(file.file_name) as XmlProperties;
                }
                else
                {
                    throw new Exception("Only support PDF and XML file type");
                }
                listFiles.Add(file);    
            }
            //ky
            FileSigned[] listSigned = signService.SignRemote(listFiles.ToArray());
            //xu ly ket qua va update vao db
            bool isAllSigned = false;
            foreach(FileSigned signed in listSigned)
            {
                if(signed.status != "success")
                {
                    //loi to khai update database ho so trang thai loi ky luon
                    var updateTKDTO = new UpdateToKhaiDto
                    {
                        TrangThai = TrangThaiFile.KyLoi,
                        ErrMsg = signed.error_message,
                    };
                    _coreService.UpdateToKhai(updateTKDTO);
                    //tk ky loi thi se update hs ky loi luon
                    var updateHSDTO = new UpdateHoSoDto
                    {
                        ListId = new string[] { hs.guid },
                        TrangThai = TrangThaiHoso.KyLoi,
                        ErrMsg = $"Lỗi ký tờ khai: {signed.error_message}",
                        FilePath = ""
                    };
                    isAllSigned = false;
                    break;
                }
                //neu ky thanh cong het gan bien isAllSigned = true
                isAllSigned = true;
            }
            if (isAllSigned == true)
            {
                //luu file va update toan bo metadata to khai vao database
                string hsPath = Path.Combine(, hs.guid);

            }

        }
    }
}
