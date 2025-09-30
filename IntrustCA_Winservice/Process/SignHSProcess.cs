using System;
using System.Linq;
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
                    ProcessMessage(ea);
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

        private void ProcessMessage(BasicDeliverEventArgs ea)
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
            //tao listfile de ky
            FileToSignDto<FileProperties>[] listFiles = CreateListFileToSign(hs);   
            //ky
            FileSigned[] listSigned = signService.SignRemote(listFiles);
            //xu ly ket qua va update vao db
            bool isAllSigned = true;
            //string hsPath = Path.Combine(Utilities.globalPath.SignedTempFolder, hs.guid);
            foreach (FileSigned signed in listSigned)
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
                    isAllSigned = false;
                    break;
                }
                //ky thanh cong thi ghi ra file va update database
                string fileId = signed.file_id;
                ToKhai tk = hs.toKhais.FirstOrDefault(t => t.TransactionId == fileId);
                try
                {
                    File.WriteAllBytes(tk.FilePath, signed.content_file.Base64ToData());
                    var updateTKDTO = new UpdateToKhaiDto
                    {
                        Id = tk.Id,
                        TrangThai = TrangThaiFile.DaKy,
                    };
                    _coreService.UpdateToKhai(updateTKDTO);
                }
                catch (Exception ex)
                {
                    Utilities.logger.ErrorLog(ex, "Failed to save signed file");
                    var updateTKDTO = new UpdateToKhaiDto
                    {
                        Id = tk.Id,
                        TrangThai = TrangThaiFile.KyLoi,
                        ErrMsg = ex.Message,
                    };
                    _coreService.UpdateToKhai(updateTKDTO);
                    isAllSigned = false;
                    break;
                }
            }
            if(isAllSigned == false)
            {
                //tk ky loi thi se hs se ky loi luon throw excepttion de nack
                //hs ky loi sau so lan nhat dinh se chuyen sang dlq
                throw new Exception("Error while signing file");
            }
            try
            {
                //neu da ky xong het ho so thi tao file BHXHDienTu
                string pathBHXHDT = CreateFileBHXHDienTu(hs);
                if(pathBHXHDT == "")
                {
                    throw new Exception("Error while creating file BHXHDienTu.xml");
                }
                //ky file BHXHDienTu
                bool isSigned = signService.SignRemoteOneFile(pathBHXHDT, pathBHXHDT);
                if (isSigned == false)
                {
                    throw new Exception("Sign BHXHDienTu.xml failed");
                }
                //thanh cong thi update db ket thuc ky file
                var updateHSDTO2 = new UpdateHoSoDto
                {
                    ListId = new string[] { hs.guid },
                    TrangThai = TrangThaiHoso.DaKy,
                    FilePath = pathBHXHDT,
                };
                _coreService.UpdateHS(updateHSDTO2);
            }
            catch (Exception ex)
            {
                Utilities.logger.ErrorLog(ex, "Error while signing file BHXHDienTu.xml", hs.guid);
                throw new Exception($"Error while signing file BHXHDienTu.xml of {hs.guid}");
            }
        }

        private FileToSignDto<FileProperties>[] CreateListFileToSign(HoSoMessage hs)
        {
            List<FileToSignDto<FileProperties>> listFiles = new List<FileToSignDto<FileProperties>>();
            foreach (ToKhai tk in hs.toKhais)
            {
                FileToSignDto<FileProperties> file = new FileToSignDto<FileProperties>();
                file.file_id = Guid.NewGuid().ToString();
                tk.TransactionId = file.file_id; //gan transactionId de sau nay truy van ket qua
                file.file_name = Path.GetFileName(tk.FilePath);
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
            return listFiles.ToArray();
        }

        private string CreateFileBHXHDienTu(HoSoMessage hs)
        {
            string HSPath = Path.Combine(Utilities.globalPath.SignedTempFolder, hs.guid);
            List<FileToKhai> listTK = new List<FileToKhai>();
            foreach (ToKhai signedTK in hs.toKhais)
            {
                byte[] tkDaKy = File.ReadAllBytes(signedTK.FilePath);
                string base64Data = Convert.ToBase64String(tkDaKy);
                string tenFile = signedTK.TenToKhai;

                FileToKhai ftk = new FileToKhai()
                {
                    MaToKhai = MethodLibrary.GetMaTK(tenFile),
                    MoTaToKhai = signedTK.MoTaToKhai,
                    TenFile = tenFile,
                    LoaiFile = Path.GetExtension(tenFile),
                    DoDaiFile = base64Data.Length,
                    NoiDungFile = base64Data,
                };
                listTK.Add(ftk);
            }
            ToKhais toKhais = new ToKhais()
            {
                FileToKhai = listTK.ToArray()
            };

            ThongTinDonVi donVi = new ThongTinDonVi()
            {
                TenDoiTuong = hs.tenDV,
                MaSoBHXH = hs.MDV,
                MaSoThue = hs.MST,
                LoaiDoiTuong = hs.loaiDoiTuong,
                NguoiKy = hs.nguoiKy,
                DienThoai = hs.dienThoai,
                CoQuanQuanLy = hs.maCQBHXH,
            };

            ThongTinIVAN iVAN = new ThongTinIVAN()
            {
                MaIVAN = "00040",
                TenIVAN = "Công ty THái Sơn",
            };

            ThongTinHoSo thongTinHoSo = new ThongTinHoSo()
            {
                TenThuTuc = hs.tenHS,
                MaThuTuc = hs.maNV,
                KyKeKhai = DateTime.Now.ToString("MM/yyyy"),
                NgayLap = DateTime.Now.ToString("dd/MM/yyyy"),
                SoLuongFile = listTK.Count(),
                QuyTrinhISO = "",
                ToKhais = toKhais,
            };

            NoiDung noiDung = new NoiDung()
            {
                ThongTinIVAN = iVAN,
                ThongTinDonVi = donVi,
                ThongTinHoSo = thongTinHoSo
            };
            Hoso hoso = new Hoso()
            {
                NoiDung = noiDung,
            };
            string pathBHXHDT = Path.Combine(HSPath, "BHXHDienTu.xml");
            MethodLibrary.SerializeToFile(hoso, pathBHXHDT);
            return pathBHXHDT;
        }
    }
}
