using ERS_Domain;
using ERS_Domain.CAService;
using ERS_Domain.clsUtilities;
using ERS_Domain.Exceptions;
using ERS_Domain.Model;
using ERS_Domain.Response;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Runtime;
using System.ServiceProcess;
using System.Text;
using System.Timers;
using System.Xml;
using System.Xml.Serialization;
using VnptHashSignatures.Common;
using VnptHashSignatures.Interface;
using VnptHashSignatures.Xml;

namespace ws_GetResult_RemoteSigning
{
    public partial class ServiceGetResult_VNPT : ServiceBase
    {
        private DbService _dbService;
        private SmartCAService _smartCAService;

        #region Timer Lay ket qua to khai
        private Timer _GetResultTKTimer;
        //thoi gian chay tu dong cua timer co the dieu chinh, mac dinh la 0.1s
        private int _tkTimeInterval = int.Parse(ConfigurationManager.AppSettings["TK_TIME_INTERVAL"]);
        //biến quy định 1 lần timer tick sẽ xử lý bao nhiêu file mặc định là 10
        private int FileCount = int.Parse(ConfigurationManager.AppSettings["FILE_COUNT"]);
        #endregion

        #region timer lay ket qua ho so
        private Timer _GetResultHSTimer;
        //thoi gian chay tu dong cua timer co the dieu chinh, mac dinh la 0.1s
        private int _hsTimeInterval = int.Parse(ConfigurationManager.AppSettings["HS_TIME_INTERVAL"]);
        //biến quy định 1 lần timer tick sẽ xử lý bao nhiêu file mặc định là 3
        private int HoSoCount = int.Parse(ConfigurationManager.AppSettings["HOSO_COUNT"]);

        #endregion

        public ServiceGetResult_VNPT()
        {
            InitializeComponent();
            _dbService = new DbService();
            _smartCAService = new SmartCAService(Utilities.glbVar.ConfigRequest);

        }

        //test method
        public void SetStart(string[] args)
        {
            this.OnStart(args);
        }

        public void SetStop()
        {
            _GetResultTKTimer.Enabled = false;
            this.OnStop();
        }

        protected override void OnStart(string[] args)
        {
            Utilities.logger.InfoLog("OnStart", "Service started");
            try
            {
                _GetResultTKTimer = new Timer();
                _GetResultTKTimer.Interval = _tkTimeInterval;
                _GetResultTKTimer.AutoReset = true;
                _GetResultTKTimer.Elapsed += TKTimer_Elapsed;
                _GetResultTKTimer.Enabled = true;

                _GetResultHSTimer = new Timer();
                _GetResultHSTimer.Interval = _hsTimeInterval;
                _GetResultHSTimer.AutoReset = true;
                _GetResultHSTimer.Elapsed += HSTimer_Elapsed;
            }
            catch (Exception ex)
            {
                Utilities.logger.ErrorLog(ex, "OnStart");
                this.Stop();
            }
        }

        protected override void OnStop()
        {
            Utilities.logger.InfoLog("OnStop", "Service stopped");
        }

        private void TKTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            _GetResultTKTimer.Enabled = false;
            try
            {
                getResultToKhai_VNPT();
            }
            catch (Exception ex)
            {
                Utilities.logger.ErrorLog(ex, "TKTimer_Elapsed");
            }
            finally
            {
                _GetResultTKTimer.Enabled = true;
            }
        }

        private void HSTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            _GetResultHSTimer.Enabled = false;
            try
            {
                GetResultHoSo_VNPT();
            }
            catch (Exception ex)
            {
                Utilities.logger.ErrorLog(ex, "HSTimer_Elapsed");
            }
            finally
            {
                _GetResultHSTimer.Enabled = true;
            }
        }

        private void getResultToKhai_VNPT()
        {
            List<int> listToKhaiId = new List<int>();
            try
            {
                //lay cac ban ghi to khai da ky hash
                //sau khi lay ket qua thi 10s sau ms lay lai ket qua neu ko thanh cong
                string TSQL = $"SELECT TOP {FileCount} * FROM ToKhai_VNPT WITH (NOLOCK) WHERE TrangThai=1 AND LastGet <= DATEADD(SECOND,-10,GETDATE()) ORDER BY NgayGui";
                DataTable dt = _dbService.GetDataTable(TSQL);
                if (dt.Rows.Count == 0)
                {
                    return;
                }
                foreach (DataRow dr in dt.Rows)
                {
                    string GuidHS = MethodLibrary.SafeString(dr["GuidHS"]);
                    int id = MethodLibrary.SafeNumber<int>(dr["id"]);
                    DateTime LastGet = MethodLibrary.SafeDateTime(dr["LastGet"]);
                    string tran_id = MethodLibrary.SafeString(dr["transaction_id"]);
                    string tenToKhai = MethodLibrary.SafeString(dr["TenToKhai"]);
                    string signerPath = MethodLibrary.SafeString(dr["SignerPath"]);
                    string url = $"https://rmgateway.vnptit.vn/sca/sp769/v1/signatures/sign/{tran_id}/status";

                    //them vao listid de update solan lay ketqua
                    listToKhaiId.Add(id);

                    ResStatus res = _smartCAService.GetStatus(url);
                    // neu ko tra ve res cho chay lay tiep cac ket qua khac
                    if (res == null) continue;

                    if (res.message == "EXPIRED")
                    {
                        //khi to khai da het han update trang thai
                        UpdateStatusToKhai(id, TrangThaiFile.HetHan, "Tờ khai đã hết hạn để ký xác nhận");
                        continue;
                    }
                    IHashSigner signer = null;
                    bool isSigned = false;
                    string pathSaved = Path.Combine(Utilities.globalPath.SignedTempFolder, GuidHS, tenToKhai);
                    switch (Path.GetExtension(tenToKhai))
                    {
                        case ".pdf":
                            isSigned = GetResult_PDF(res.data, pathSaved);
                            break;
                        case ".xml":
                            signer = RestoreSignerXML(signerPath);
                            if (signer == null)
                            {
                                UpdateStatusToKhai(id, TrangThaiFile.KyLoi, "Không tạo được signer");
                                continue;
                            }
                            isSigned = GetResult_Xml(signer, res.data, pathSaved);
                            break;
                        case ".docx":
                        case ".xlsx":
                            //tao lai signer
                            signer = RestoreSignerOffice(signerPath);
                            if (signer == null)
                            {
                                //update trang thai la ky loi
                                UpdateStatusToKhai(id, TrangThaiFile.KyLoi, "Không tạo được signer");
                                continue;
                            }
                            isSigned = GetResult_Office(signer, res.data, pathSaved);
                            break;
                        default:
                            //khi tiep nhan file o webservice la da kiem tra kieu file
                            throw new Exception("Không hỗ trợ kiểu file ký");
                    }
                    if (!isSigned)
                    {
                        continue;
                    }
                    //update thanh trang thai da ky
                    UpdateStatusToKhai(id, TrangThaiFile.DaKy,"", pathSaved);
                }
                //update so lan lay ket qua cua nhung ho so da goi lay ket qua, nhung id nay 10 s sau moi lay tiep ket qua
                UpdateLastGetToKhai(listToKhaiId);
            }
            catch(DatabaseInteractException ex)
            {
                //neu co loi database thi dung luon ham
                Utilities.logger.ErrorLog(ex, "Lỗi tương tác database");
                return;
            }
            catch (Exception ex)
            {
                Utilities.logger.ErrorLog(ex, "GetResultVNPT");
            }
        }

        //import lai thong tin signer da luu tru
        private SignerInfo ImportSigner(string filePath)
        {
            string json = File.ReadAllText(filePath);
            SignerInfo output = JsonConvert.DeserializeObject<SignerInfo>(json);
            return output;
        }

        private IHashSigner RestoreSignerOffice(string signerPath)
        {
            try
            {
                SignerInfo signerInfo = ImportSigner(signerPath);
                if (signerInfo == null)
                {
                    return null;
                }
                IHashSigner signer = null;

                signer = HashSignerFactory.GenerateSigner(signerInfo.UnsignData, signerInfo.SignerCert, null, HashSignerFactory.OFFICE);
                signer.SetHashAlgorithm(MessageDigestAlgorithm.SHA256);
                signer.GetSecondHashAsBase64();
                return signer;
            }
            catch (Exception ex)
            {
                Utilities.logger.ErrorLog(ex, "RestoreSignerOffice");
                return null;
            }
        }

        private IHashSigner RestoreSignerXML(string signerPath, bool isTokhai = true)
        {
            try
            {
                SignerInfo signerInfo = ImportSigner(signerPath);
                if (signerInfo == null)
                {
                    return null;
                }
                IHashSigner signer = HashSignerFactory.GenerateSigner(signerInfo.UnsignData, signerInfo.SignerCert, null, HashSignerFactory.XML);
                signer.SetHashAlgorithm(MessageDigestAlgorithm.SHA256);

                ((XmlHashSigner)signer).SetSignatureID(Guid.NewGuid().ToString());
                ((XmlHashSigner)signer).SetSigningTime(DateTime.Now, "SigningTime-" + Guid.NewGuid().ToString());
                if (isTokhai)
                {
                    ((XmlHashSigner)signer).SetParentNodePath("//Cky");
                }
                else
                {
                    ((XmlHashSigner)signer).SetParentNodePath("/Hoso/CKy_Dvi");
                }
                signer.GetSecondHashAsBase64();
                return signer;
            }
            catch (Exception ex)
            {
                Utilities.logger.ErrorLog(ex, "");
                return null;
            }
        }

        private bool GetResult_PDF(DataTransaction transactionStatus, string PDFSignedPath)
        {
            var isConfirm = false;
            var datasigned = "";
            var mapping = "";
            string tempFolder = Path.GetTempPath();
            if (transactionStatus.signatures != null)
            {
                datasigned = transactionStatus.signatures[0].signature_value;
                mapping = transactionStatus.signatures[0].doc_id;
                isConfirm = true;
            }

            if (!isConfirm)
            {
                return false;
            }

            if (string.IsNullOrEmpty(datasigned))
            {
                Utilities.logger.ErrorLog("Confirmed sign error", transactionStatus.transaction_id);
                return false;
            }

            string contentTempFile = File.ReadAllText(tempFolder + mapping + ".txt");
            SignerProfile signerProfileNew = JsonConvert.DeserializeObject<SignerProfile>(contentTempFile);
            var signer1 = HashSignerFactory.GenerateSigner(signerProfileNew.DocType);
            if (!signer1.CheckHashSignature(signerProfileNew, datasigned))
            {
                Utilities.logger.ErrorLog("Signature not match", transactionStatus.transaction_id);
                return false;
            }

            byte[] signed = signer1.Sign(signerProfileNew, datasigned);
            try
            {
                File.WriteAllBytes(PDFSignedPath, signed);
            }
            catch (UnauthorizedAccessException ex)
            {
                Utilities.logger.ErrorLog(ex, "UnauthorizedAccessException");
                return false;
            }
            return true;
        }
        private bool GetResult_Office(IHashSigner signer, DataTransaction transactionStatus, string officeSignedPath)
        {
            var isConfirm = false;
            var datasigned = "";
            if (transactionStatus.signatures != null)
            {
                datasigned = transactionStatus.signatures[0].signature_value;
                isConfirm = true;
            }

            if (!isConfirm)
            {
                return false;
            }

            if (string.IsNullOrEmpty(datasigned))
            {
                return false;
            }

            byte[] signed = signer.Sign(datasigned);
            File.WriteAllBytes(officeSignedPath, signed);
            return true;
        }

        private bool GetResult_Xml(IHashSigner signer, DataTransaction transactionStatus, string xmlSignedPath)
        {
            var isConfirm = false;
            var datasigned = "";

            if (transactionStatus.signatures != null)
            {
                datasigned = transactionStatus.signatures[0].signature_value;
                isConfirm = true;
            }

            if (!isConfirm)
            {
                return false;
            }

            if (string.IsNullOrEmpty(datasigned))
            {
                //Console.WriteLine("Sign error");
                return false;
            }

            if (!signer.CheckHashSignature(datasigned))
            {
                Utilities.logger.ErrorLog("Không thể valid chữ ký số", transactionStatus.transaction_id);
                return false;
            }

            byte[] signed = signer.Sign(datasigned);
            File.WriteAllBytes(xmlSignedPath, signed);
            return true;
        }

        private void UpdateStatusToKhai(int id, TrangThaiFile TrangThai, string errMsg = "", string PathFile = "")
        {
            bool result = _dbService.ExecQuery("UPDATE ToKhai_VNPT SET TrangThai=@TrangThai, ErrMsg=@ErrMsg, FilePath=@FilePath WHERE id=@id", "", new SqlParameter[]
                {
                new SqlParameter("@TrangThai", (int)TrangThai),
                new SqlParameter("@ErrMsg", errMsg),
                new SqlParameter("@FilePath", PathFile),
                new SqlParameter("@id", id),
                });
            if (!result)
            {
                throw new DatabaseInteractException($"Có lỗi khi update trạng thái tờ khai: {id}");
            }
        }

        private void UpdateLastGetToKhai(List<int> listToKhaiId)
        {
            if (listToKhaiId.Count > 0)
            {
                string strListId = string.Join(",", listToKhaiId);
                string TSQL = $"UPDATE ToKhai_VNPT SET LastGet=@LastGet WHERE id IN ({strListId})";
                var result = _dbService.ExecQuery(TSQL, "", new SqlParameter[]
                {
                        new SqlParameter("@LastGet", DateTime.Now)
                });
                if (!result)
                {
                    throw new DatabaseInteractException($"Có lỗi khi update các tờ khai: {strListId}");
                }
            }
        }

        #region HoSo region
        private void SignFileBHXH()
        {
            try
            {
                //select trong bang HoSo_VNPT
                string TSQL = $"SELECT TOP {HoSoCount} * FROM HoSo_VNPT WITH (NOLOCK) WHERE GUID IN (SELECT GUID FROM HoSo_VNPT A JOIN ToKhai_VNPT B ON A.Guid = B.GuidHS GROUP BY A.Guid HAVING COUNT(*) = SUM(CASE WHEN B.TrangThai = 2 THEN 1 ELSE 0 END)) ORDER BY NgayGui";
                DataTable dtHS = _dbService.GetDataTable(TSQL);
                if (dtHS.Rows.Count == 0) return;
                foreach(DataRow dr in dtHS.Rows)
                {
                    string GuidHS = MethodLibrary.SafeString(dr["Guid"]);
                    string pathSaveHS = Path.Combine(Utilities.globalPath.SignedTempFolder, $"{GuidHS}");
                    bool isCreated = CreateBHXHDienTu(dr,pathSaveHS);
                    if (!isCreated)
                    {
                        //ko tao dc file thi continue lan sau tao lai
                    }
                }
            }
            catch (Exception)
            {

                throw;
            }
        }

        private bool CreateBHXHDienTu(DataRow dr, string pathFolderHoSo)
        {
            try
            {
                string GuidHS = MethodLibrary.SafeString(dr["Guid"]);
                DataTable dtToKhais = _dbService.GetDataTable("SELECT * FROM ToKhai_VNPT WITH (NOLOCK) WHERE GuidHS=@GuidHS", "", new SqlParameter[]
                {
                        new SqlParameter("@GuidHS", GuidHS)
                });
                if (dtToKhais.Rows.Count == 0) return false;
                List<FileToKhai> listTK = new List<FileToKhai>();
                foreach (DataRow rowTK in dtToKhais.Rows)
                {
                    byte[] tkDaKy = File.ReadAllBytes(MethodLibrary.SafeString(rowTK["FilePath"]));
                    string base64Data = Convert.ToBase64String(tkDaKy);
                    string tenFile = MethodLibrary.SafeString(rowTK["TenToKhai"]);

                    FileToKhai tk = new FileToKhai()
                    {
                        MaToKhai = GetMaTK(tenFile),
                        MoTaToKhai = MethodLibrary.SafeString(rowTK["MoTa"]),
                        TenFile = tenFile,
                        LoaiFile = Path.GetExtension(tenFile),
                        DoDaiFile = base64Data.Length,
                        NoiDungFile = base64Data,
                    };
                    listTK.Add(tk);
                }
                ToKhais toKhais = new ToKhais()
                {
                    FileToKhai = listTK.ToArray()
                };

                ThongTinDonVi donVi = new ThongTinDonVi()
                {
                    TenDoiTuong = MethodLibrary.SafeString(dr["TenDonVi"]),
                    MaSoBHXH = MethodLibrary.SafeString(dr["FromMDV"]),
                    MaSoThue = MethodLibrary.SafeString(dr["FromMST"]),
                    LoaiDoiTuong = MethodLibrary.SafeNumber<int>(dr["LoaiDoiTuong"]),
                    NguoiKy = MethodLibrary.SafeString(dr["NguoiKy"]),
                    DienThoai = MethodLibrary.SafeString(dr["DienThoai"]),
                    CoQuanQuanLy = MethodLibrary.SafeString(dr["MaCQBH"]),
                };

                ThongTinIVAN iVAN = new ThongTinIVAN()
                {
                    MaIVAN = "00040",
                    TenIVAN = "Công ty THái Sơn",
                };

                ThongTinHoSo thongTinHoSo = new ThongTinHoSo()
                {
                    TenThuTuc = MethodLibrary.SafeString(dr["TenHS"]),
                    MaThuTuc = MethodLibrary.SafeString(dr["MaNV"]),
                    KyKeKhai = DateTime.Now.ToString("MM/yyyy"),
                    NgayLap = DateTime.Now.ToString("dd/MM/yyyy"),
                    SoLuongFile = dtToKhais.Rows.Count,
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

                XmlSerializer serializer = new XmlSerializer(typeof(Hoso));
                XmlWriterSettings settings = new XmlWriterSettings()
                {
                    Encoding = new UTF8Encoding(true),
                    Indent = true
                };

                string xmlString = "";
                using (var stream = new MemoryStream())
                using (var writer = XmlWriter.Create(stream, settings))
                {
                    serializer.Serialize(writer, hoso);
                    xmlString = Encoding.UTF8.GetString(stream.ToArray());
                }
                string pathBHXHDT = Path.Combine(pathFolderHoSo, "BHXHDienTu.xml");
                File.WriteAllText(pathBHXHDT, xmlString);
                return true;
            }
            catch (Exception ex)
            {
                Utilities.logger.ErrorLog(ex , "CreateBHXHDienTu");
                return false;
            }
        }

        private string GetMaTK(string tenFile)
        {
            string[] extensions = { ".pdf",".docx",".xlsx"};
            if (extensions.Contains(Path.GetExtension(tenFile)))
            {
                return "CT-DK";
            }
            else
            {
                return Path.GetExtension(tenFile).Replace("-595", "");
            }
        }

        private void GetResultHoSo_VNPT()
        {
            try
            {


            }
            catch (Exception)
            {

                throw;
            }
        }
        #endregion
    }
}
