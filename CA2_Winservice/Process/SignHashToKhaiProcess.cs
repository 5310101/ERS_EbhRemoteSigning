using CA2_Winservice.Cache;
using CA2_Winservice.Services;
using ERS_Domain;
using ERS_Domain.CAService;
using ERS_Domain.clsUtilities;
using ERS_Domain.CustomSigner.CA2CustomSigner;
using ERS_Domain.Dtos;
using ERS_Domain.Model;
using ERS_Domain.Request;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using System.Xml;

namespace CA2_Winservice.Process
{
    public class SignHashToKhaiProcess
    {
        private readonly IChannel _channel;
        private readonly CoreService _coreService;
        private readonly CA2SigningService _ca2Service;
        //so luong ky hash to khai tren moi process cho 1 queue
        private readonly ushort _numberHSPerProcess = ushort.Parse(System.Configuration.ConfigurationManager.AppSettings["SIGNHASH_TOKHAI_PERPROCESS"]);    

        public SignHashToKhaiProcess(IChannel channel, CoreService coreService, CA2SigningService ca2Service)
        {
            _channel = channel;
            _coreService = coreService;
            _ca2Service = ca2Service;

            //declare queue
            _channel.QueueDeclareAsync("HSCA2.ToKhai.dlq",true, false, false, null).GetAwaiter().GetResult();
            //Ky hash loi van cho retry, nhung khi lay ket qua signature value thi ko cho retry nua
            _channel.QueueDeclareAsync("HSCA2.ToKhai.retry.q", true, false,false,
                RabbitMQHelper.CreateQueueArgument("","HSCA2.ToKhai.q",true));
            _channel.QueueDeclareAsync("HSCA2.ToKhai.q", true, false, false,
                RabbitMQHelper.CreateQueueArgument("", "HSCA2.ToKhai.dlq", false)).GetAwaiter().GetResult();
            //queue dung de day lai to khai khi nguoi dung chua ky tren app
            _channel.QueueDeclareAsync(
                "HSCA2.ToKhai.GetResult.retry.q", true, false, false, RabbitMQHelper.CreateQueueArgument("", "HSCA2.ToKhai.q", true)
                ).GetAwaiter().GetResult();

            _channel.QueueDeclareAsync("HSCA2.ReadyToSign.q", true, false, false,
               RabbitMQHelper.CreateQueueArgument("", "HSCA2.ReadyToSign.dlq", false)).GetAwaiter().GetResult();
            //ko lay dc ket qa thi sau 5s se retry
            _channel.QueueDeclareAsync("HSCA2.ReadyToSign.retry.q", true, false, false,
                RabbitMQHelper.CreateQueueArgument("", "HSCA2.ReadyToSign.q", true)).GetAwaiter().GetResult();
            _channel.QueueDeclareAsync("HSCA2.ReadyToSign.dlq", true, false, false, null).GetAwaiter().GetResult();
            //chi dinh queue retry hs khi chua ky tu nguoi dung
            _channel.QueueDeclareAsync("HSCA2.ReadyToSign.GetResult.retry.q", true, false, false,
                RabbitMQHelper.CreateQueueArgument("", "HSCA2.ReadyToSign.q", true)).GetAwaiter().GetResult();
        }

        public void StartProcess()
        {
            _channel.BasicQosAsync(0, _numberHSPerProcess, false).GetAwaiter().GetResult();
            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += async(model, ea) =>
            {
                try
                {
                    await ProcessMessage(ea);   
                    await _channel.BasicAckAsync(ea.DeliveryTag, false);
                }
                catch (Exception ex)
                {
                    Utilities.logger.ErrorLog(ex, "Consume message error", "SignHashToKhaiProcess");
                    await RabbitMQHelper.HandleError(_channel, ea, 3, "HSCA2.retry.q", ex, _coreService.UpdateHS);
                }
            };
            _channel.BasicConsumeAsync("HSCA2.q", false, consumer).GetAwaiter().GetResult();
        }

        public async Task ProcessMessage(BasicDeliverEventArgs ea)
        {
            var hs = ea.ProcessMessageToObject<HoSoMessage>();
            if (hs == null)
            {
                throw new Exception("Deserialize error or incorrect message");
            }
            //string transactionId = "EBH".GenGuidStr();
            var res = await _ca2Service.GetCertificates(hs.uid, "EBH".GenGuidStr(), hs.serialNumber);
            if(res == null || res?.status_code != 200)
            {
                throw new Exception("Cannot get certificate from CA2 service");
            }
            var cert = res.data.user_certificates[0];
            var x509Cert = new X509Certificate2(Convert.FromBase64String(cert.cert_data));
            DateTime signDate = DateTime.Now;
            List<FileToSign> lstTK = new List<FileToSign>();

            if(hs.typeDK == TypeHS.HSNV || hs.typeDK == TypeHS.HSDKLanDau)
            {
                if(hs.toKhais.Any() == false)
                {
                    throw new Exception($"Cannot find any files to sign in {hs.guid}");
                }
                foreach (ToKhai tk in hs.toKhais)
                {
                    
                    switch (tk.LoaiFile)
                    {
                        case FileType.PDF:
                            tk.TransactionId = "pdf".GenGuidStr();
                            var profilePDF = CA2SignUtilities.CreateHashPdfToSign(cert.cert_data, tk.FilePath, signDate, hs.guid, tk.TransactionId );
                            //luu profile
                            ProfileCache.SetProfileCache(profilePDF.DocId, profilePDF);
                            lstTK.Add(new FileToSign
                            {
                                doc_id = profilePDF.DocId,
                                file_type = "pdf",
                                data_to_be_signed = profilePDF.HashValue.ToBase64String(),
                            });
                            break;
                        case FileType.XML:
                            tk.TransactionId = "xml".GenGuidStr();
                            //XmlElement signedInfo = CA2SignUtilities.CreateSignedInfoNode(tk.FilePath,"");
                            //string hashToSignXml = CA2SignUtilities.CreateHashXmlToSign(signedInfo);
                            string hashToSignXml = CA2SignUtilities.ComputeDigestValue(tk.FilePath, x509Cert, Path.GetFileNameWithoutExtension(tk.TenToKhai).GetTagNodeSignXml(), out string tempPath);
                            CA2XMlSignerProfile profileXML = new CA2XMlSignerProfile
                            {
                                DocId = tk.TransactionId,
                                //SignedInfo = signedInfo,
                                CertData = cert.cert_data,  
                                TempPath = tempPath
                            };
                            ProfileCache.SetProfileCache(profileXML.DocId, profileXML);
                            lstTK.Add(new FileToSign
                            {
                                doc_id = profileXML.DocId,
                                file_type = "xml",
                                data_to_be_signed = hashToSignXml,
                            }); 
                            break;
                        default:
                            throw new Exception("File type is not supported");
                    }
                }
                //tao 1 ma transactionId cho giao dich ky
                hs.transactionId = "".GenGuidStr();
                var result = await _ca2Service.SignHashValue(hs.uid, hs.transactionId, lstTK.ToArray(), hs.serialNumber, signDate);
                if (result == null || result?.status_code != 200)
                {
                    throw new Exception("Cannot sign hash value from CA2 service");
                }
                //Neu ko loi push message sang queue tiep theo
                //da ky hash thanh cong thi update trang thai to khai
                foreach (var tk in hs.toKhais)
                {
                    var updateTK = new UpdateToKhaiDto
                    {
                        Id = tk.Id,
                        TrangThai = TrangThaiFile.DaKyHash
                    };
                    _coreService.UpdateToKhai(updateTK);
                }
                //day sang queue tiep theo
                await _channel.BasicPublishAsync("", "HSCA2.ToKhai.q", false, new BasicProperties
                {
                    DeliveryMode = DeliveryModes.Persistent,
                }, hs.GetBytesStringFromJsonObject());
            }
            else if(hs.typeDK == TypeHS.HSDK)
            {
                //hs dang ky se ko co to khai
                //ky hash cho hoso dang ky
                string pathFileHSDK = Path.Combine(Utilities.globalPath.SignedTempFolder, hs.guid, $"{hs.maNV}.xml");
                hs.transactionId = "HSDK".GenGuidStr();
                hs.filePathHS = pathFileHSDK;
                //XmlElement signedInfo = CA2SignUtilities.CreateSignedInfoNode(pathFileHSDK, "");
                //string hashToSignXml = CA2SignUtilities.CreateHashXmlToSign(signedInfo);
                string hashToSignXml = CA2SignUtilities.ComputeDigestValue(pathFileHSDK, x509Cert, Path.GetFileNameWithoutExtension(pathFileHSDK).GetTagNodeSignXml(), out string tempPathHSDK);
                CA2XMlSignerProfile profileXML = new CA2XMlSignerProfile
                {
                    DocId = hs.transactionId,
                    TempPath = tempPathHSDK,
                    CertData = cert.cert_data,
                };
                ProfileCache.SetProfileCache(profileXML.DocId, profileXML);
                FileToSign fts = new FileToSign
                {
                    doc_id = profileXML.DocId,
                    file_type = "xml",
                    data_to_be_signed = hashToSignXml,
                };
                var resHSDK = await _ca2Service.SignHashValue(hs.uid, hs.transactionId, new FileToSign[] { fts }, hs.serialNumber, signDate);
                if(resHSDK == null || res?.status_code != 200)
                {
                    throw new Exception("Cannot sign hash value of HSDK from CA2 service");
                }
                //da ky hash thanh cong thi update trang thai hoso
                var hsUpdateDto = new UpdateHoSoDto
                {
                    ListId = new string[] { hs.guid },
                    TrangThai = TrangThaiHoso.DaKyHash,
                    FilePath = pathFileHSDK,
                };
                _coreService.UpdateHS(hsUpdateDto);
                await _channel.BasicPublishAsync("", "HSCA2.ReadyToSign.q", false, new BasicProperties
                {
                    DeliveryMode = DeliveryModes.Persistent,
                }, hs.GetBytesStringFromJsonObject());  
            }
            else
            {
                throw new Exception("TypeHS is not valid");
            }
            
        }
    }
}
