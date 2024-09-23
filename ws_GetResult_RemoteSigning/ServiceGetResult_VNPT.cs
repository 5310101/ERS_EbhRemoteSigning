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
        private Timer _getResultTKTimer;
        //thoi gian chay tu dong cua timer co the dieu chinh, mac dinh la 0.1s
        private int _tkTimeInterval = int.Parse(ConfigurationManager.AppSettings["TK_TIME_INTERVAL"]);
        //biến quy định 1 lần timer tick sẽ xử lý bao nhiêu file mặc định là 10
        private int FileCount = int.Parse(ConfigurationManager.AppSettings["FILE_COUNT"]);
        #endregion

        #region Timer ky hash file BHXHDienTu.xml
        private Timer _signHSTimer;
        //thoi gian chay tu dong cua timer co the dieu chinh, mac dinh la 0.1s
        private int _signHSTimeInterval = int.Parse(ConfigurationManager.AppSettings["SIGNHS_TIME_INTERVAL"]);
        //biến quy định 1 lần timer tick sẽ xử lý bao nhiêu file mặc định là 3
        private int HSSignCount = int.Parse(ConfigurationManager.AppSettings["SIGNHS_COUNT"]);
        #endregion

        #region timer lay ket qua ho so
        private Timer _getResultHSTimer;
        //thoi gian chay tu dong cua timer co the dieu chinh, mac dinh la 0.1s
        private int _hsTimeInterval = int.Parse(ConfigurationManager.AppSettings["HS_TIME_INTERVAL"]);
        //biến quy định 1 lần timer tick sẽ xử lý bao nhiêu file mặc định là 10
        private int HoSoCount = int.Parse(ConfigurationManager.AppSettings["HOSO_COUNT"]);

        #endregion

        private readonly string SignedTempFolder = ConfigurationManager.AppSettings["HOSO_TEMP_FOLDER"];

        public ServiceGetResult_VNPT()
        {
            InitializeComponent();
            _dbService = new DbService();
            _smartCAService = new SmartCAService(Utilities.glbVar.ConfigRequest);

        }

        #region test method
        public void SetStart(string[] args)
        {
            this.OnStart(args);
        }

        public void SetStop()
        {
            _getResultTKTimer.Enabled = false;
            this.OnStop();
        }
        #endregion

        protected override void OnStart(string[] args)
        {
            Utilities.logger.InfoLog("OnStart", "Service started");
            try
            {
                _getResultTKTimer = new Timer();
                _getResultTKTimer.Interval = _tkTimeInterval;
                _getResultTKTimer.AutoReset = true;
                _getResultTKTimer.Elapsed += TKTimer_Elapsed;
                _getResultTKTimer.Enabled = true;

                _signHSTimer = new Timer();
                _signHSTimer.Interval = _signHSTimeInterval;
                _signHSTimer.AutoReset = true;
                _signHSTimer.Elapsed += SignHSTimer_Elapsed;
                _signHSTimer.Enabled = true;

                _getResultHSTimer = new Timer();
                _getResultHSTimer.Interval = _hsTimeInterval;
                _getResultHSTimer.AutoReset = true;
                _getResultHSTimer.Elapsed += HSTimer_Elapsed;
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
            _getResultTKTimer.Enabled = false;
            try
            {
                GetResultToKhai_VNPT();
            }
            catch (Exception ex)
            {
                Utilities.logger.ErrorLog(ex, "TKTimer_Elapsed");
            }
            finally
            {
                _getResultTKTimer.Enabled = true;
            }
        }

        private void SignHSTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            _signHSTimer.Enabled = false;
            try
            {
                SignFileBHXHDienTu();
            }
            catch (Exception ex)
            {
                Utilities.logger.ErrorLog(ex, "SignHSTimer_Elapsed");
            }
            finally
            {
                _signHSTimer.Enabled = true;
            }
        }


        private void HSTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            _getResultHSTimer.Enabled = false;
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
                _getResultHSTimer.Enabled = true;
            }
        }

        private void GetResultToKhai_VNPT()
        {
            List<int> listIdChuaCoKetQua = new List<int>();
            //lay cac ban ghi to khai da ky hash
            //sau khi lay ket qua thi 10s sau ms lay lai ket qua neu ko thanh cong
            string TSQL = $"SELECT TOP {FileCount} * FROM ToKhai_VNPT WITH (NOLOCK) WHERE TrangThai=1 AND LastGet <= DATEADD(SECOND,-10,GETDATE()) ORDER BY NgayGui";
            try
            {
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
                    try
                    {

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
                        //thu muc duong dan save file to khai sau khi ky

                        string pathSaved = Path.Combine(SignedTempFolder, GuidHS, tenToKhai);
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
                            //voi nhung truong hop ko loi ma chua lay dc ket qua thi se ko update trang thai ma chi update LastGet
                            listIdChuaCoKetQua.Add(id);
                            continue;
                        }
                        //update thanh trang thai da ky
                        UpdateStatusToKhai(id, TrangThaiFile.DaKy, "", pathSaved);
                    }
                    catch (DatabaseInteractException ex)
                    {
                        Utilities.logger.ErrorLog(ex, "Lỗi tương tác database");
                        continue;
                    }
                    catch (Exception ex)
                    {
                        Utilities.logger.ErrorLog(ex, $"Lỗi khi ký file {GuidHS}[{id}]");
                        continue;
                    }
                }
                UpdateLastGetToKhai(listIdChuaCoKetQua);
            }
            catch (DatabaseInteractException ex)
            {
                Utilities.logger.ErrorLog(ex, "GetResultToKhai_VNPT");
            }
        }

        private IHashSigner RestoreSignerOffice(string signerPath)
        {
            try
            {
                SignerInfo signerInfo = MethodLibrary.ImportSigner(signerPath);
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
                SignerInfo signerInfo = MethodLibrary.ImportSigner(signerPath);
                if (signerInfo == null)
                {
                    return null;
                }
                IHashSigner signer = HashSignerFactory.GenerateSigner(signerInfo.UnsignData, signerInfo.SignerCert, null, HashSignerFactory.XML);
                signer.SetHashAlgorithm(MessageDigestAlgorithm.SHA256);

                ((XmlHashSigner)signer).SetSignatureID(signerInfo.SigId);
                ((XmlHashSigner)signer).SetSigningTime(signerInfo.SigningTime, "SigningTime-" + signerInfo.SigningTimeId);
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


        #region database interact
        private void UpdateStatusToKhai(int id, TrangThaiFile TrangThai, string errMsg = "", string PathFile = "")
        {
            bool result = _dbService.ExecQuery("UPDATE ToKhai_VNPT SET TrangThai=@TrangThai, ErrMsg=@ErrMsg, FilePath=@FilePath, LastGet=@LastGet WHERE id=@id", "", new SqlParameter[]
                {
                new SqlParameter("@TrangThai", (int)TrangThai),
                new SqlParameter("@ErrMsg", errMsg),
                new SqlParameter("@FilePath", PathFile),
                new SqlParameter("@LastGet", DateTime.Now),
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

        private void UpdateStatusHoSo(string GuidHS, TrangThaiHoso TrangThai, string errMsg = "", string signerPath = "", string PathFile = "")
        {
            bool result = _dbService.ExecQuery("UPDATE HoSo_VNPT SET TrangThai=@TrangThai, ErrMsg=@ErrMsg, SignerPath=@SignerPath, FilePath=@FilePath, LastGet=@LastGet WHERE Guid=@Guid", "", new SqlParameter[]
                {
                new SqlParameter("@TrangThai", (int)TrangThai),
                new SqlParameter("@ErrMsg", errMsg),
                new SqlParameter("@SingerPath", signerPath),
                new SqlParameter("@FilePath", PathFile),
                new SqlParameter("@LastGet", DateTime.Now),
                new SqlParameter("@Guid", GuidHS),
                });
            if (!result)
            {
                throw new DatabaseInteractException($"Có lỗi khi update trạng thái hồ sơ: {GuidHS}");
            }
        }

        private void UpdateLastGetHoSo(List<int> ListIdHS)
        {
            string strListId = string.Join(",", ListIdHS);
            string TSQL = $"UPDATE HoSo_VNPT SET LastGet=@LastGet WHERE id IN ({strListId})";
            var result = _dbService.ExecQuery(TSQL, "", new SqlParameter[]
            {
                        new SqlParameter("@LastGet", DateTime.Now)
            });
            if (!result)
            {
                throw new DatabaseInteractException($"Có lỗi khi update các hồ sơ: {strListId}");
            }

        }
        #endregion 

        #region HoSo region
        private void SignFileBHXHDienTu()
        {
            try
            {
                //select trong bang HoSo_VNPT, chi select cac hoso ma tat ca to khai da dc ky(trang thai =2)
                string TSQL = $"SELECT TOP {HSSignCount} * FROM HoSo_VNPT WITH (NOLOCK) WHERE GUID IN (SELECT GUID FROM HoSo_VNPT A JOIN ToKhai_VNPT B ON A.Guid = B.GuidHS GROUP BY A.Guid HAVING COUNT(*) = SUM(CASE WHEN B.TrangThai = 2 THEN 1 ELSE 0 END)) ORDER BY NgayGui";
                DataTable dtHS = _dbService.GetDataTable(TSQL);
                if (dtHS.Rows.Count == 0) return;
                foreach (DataRow dr in dtHS.Rows)
                {
                    try
                    {
                        string GuidHS = MethodLibrary.SafeString(dr["Guid"]);
                        string uid = MethodLibrary.SafeString(dr["uid"]);
                        string serialNumber = MethodLibrary.SafeString(dr["SerialNumber"]);
                        string pathSaveHS = Path.Combine(SignedTempFolder, $"{GuidHS}");
                        bool isCreated = CreateBHXHDienTu(dr, pathSaveHS);
                        if (!isCreated)
                        {
                            //ko tao dc file thi continue lan sau tao lai
                            continue;
                        }
                        bool isSigned = SignHoSoBHXH(uid, GuidHS, serialNumber);
                        if (!isSigned)
                        {
                            Utilities.logger.InfoLog($"Hồ sơ ký lỗi không lấy kết quả: {GuidHS}", "Hồ sơ ký lỗi");
                        }
                    }
                    catch
                    {
                        continue;
                    }
                }
            }
            catch (Exception ex)
            {
                Utilities.logger.ErrorLog(ex, "SignFileBHXH");
                return;
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
                        MaToKhai = MethodLibrary.GetMaTK(tenFile),
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
                    Encoding = new UTF8Encoding(false),
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
                Utilities.logger.ErrorLog(ex, "CreateBHXHDienTu");
                return false;
            }
        }

        private bool SignHoSoBHXH(string uid, string GuidHS, string serialNumber)
        {
            try
            {
                //lay cert
                UserCertificate userCert = _smartCAService.GetAccountCert(VNPT_URI.uriGetCert_test, uid, serialNumber);
                if (userCert == null)
                {
                    Utilities.logger.ErrorLog("Không tìm thấy cert trên server VNPT để ký", "Lỗi khi ký", uid, serialNumber);
                    return false;
                }
                string PathTempHoSo = Path.Combine(SignedTempFolder, GuidHS);
                string pathBHXHDt = Path.Combine(PathTempHoSo, "BHXHDienTu.xml");
                DataSign dataSign = SignSmartCAXML(userCert, pathBHXHDt, uid, out SignerInfo signer, "/Hoso/CKy_Dvi");

                if (dataSign == null)
                {
                    //update trang thai ky loi
                    UpdateStatusHoSo(GuidHS, TrangThaiHoso.KyLoi, "Lỗi khi ký hash");
                    return false;
                }
                //co the thay doi thoi gian countdown dua vao data tra ve tu sever, default la 300
                if (signer == null)
                {
                    //update trang thai ky loi
                    UpdateStatusHoSo(GuidHS, TrangThaiHoso.KyLoi, "Không tạo được signer");
                    return false;
                }
                string PathExportSigner = MethodLibrary.ExportSigner(signer, PathTempHoSo, dataSign.transaction_id);
                if (PathExportSigner == "")
                {
                    //update trang thai ky loi
                    UpdateStatusHoSo(GuidHS, TrangThaiHoso.KyLoi, "Có lỗi khi lưu trữ signer");
                    return false;
                }
                UpdateStatusHoSo(GuidHS, TrangThaiHoso.DaKyHash, "", PathExportSigner, "");
                return true;
            }
            catch (DatabaseInteractException ex)
            {
                Utilities.logger.ErrorLog(ex.Message, "Lỗi tương tác database");
                return false;
            }
            catch (Exception ex)
            {
                Utilities.logger.ErrorLog(ex, "SignHoSoBHXH", GuidHS);
                return false;
            }
        }

        private DataSign SignSmartCAXML(UserCertificate userCert, string FileBHXHPath, string uid, out SignerInfo signerInfo, string nodeKy = "")
        {
            IHashSigner signer = null;
            signerInfo = new SignerInfo();
            try
            {
                byte[] xmlUnsign = File.ReadAllBytes(FileBHXHPath);
                String certBase64 = userCert.cert_data;
                signerInfo.SignerCert = certBase64;
                signerInfo.UnsignData = xmlUnsign;
                signer = HashSignerFactory.GenerateSigner(xmlUnsign, certBase64, null, HashSignerFactory.XML);
                signer.SetHashAlgorithm(MessageDigestAlgorithm.SHA256);

                string SignId = Guid.NewGuid().ToString();
                ((XmlHashSigner)signer).SetSignatureID(SignId);
                //Set reference đến id
                //((XmlHashSigner)signers).SetReferenceId("#SigningData");

                //Set thời gian ký
                ((XmlHashSigner)signer).SetSigningTime(DateTime.Now, "SigningTime-" + Guid.NewGuid().ToString());

                //đường dẫn dẫn đến thẻ chứa chữ ký 
                if (nodeKy == "")
                {
                    nodeKy = "//Cky";
                }
                ((XmlHashSigner)signer).SetParentNodePath(nodeKy);

                var hashValue = signer.GetSecondHashAsBase64();

                var data_to_be_sign = BitConverter.ToString(Convert.FromBase64String(hashValue)).Replace("-", "").ToLower();

                DataSign dataSign = _smartCAService.Sign(VNPT_URI.uriSign_test, data_to_be_sign, userCert.serial_number, uid);

                return dataSign;

            }
            catch (Exception ex)
            {
                Utilities.logger.ErrorLog(ex, "SignSmartCAXML", userCert.cert_subject);
                signerInfo = null;
                return null;
            }
        }

        private void GetResultHoSo_VNPT()
        {
            List<int> ListHSChuaCoKQ = new List<int>();
            //lay cac ban ghi to khai da ky hash
            //sau khi lay ket qua thi 10s sau ms lay lai ket qua neu ko thanh cong
            string TSQL = $"SELECT TOP {HoSoCount} * FROM HoSo_VNPT WITH (NOLOCK) WHERE TrangThai=1 AND LastGet <= DATEADD(SECOND,-10,GETDATE()) ORDER BY NgayGui";
            try
            {
                DataTable dt = _dbService.GetDataTable(TSQL);
                if (dt.Rows.Count == 0)
                {
                    return;
                }
                foreach (DataRow dr in dt.Rows)
                {
                    string GuidHS = MethodLibrary.SafeString(dr["Guid"]);
                    int idHS = MethodLibrary.SafeNumber<int>(dr["id"]);
                    DateTime LastGet = MethodLibrary.SafeDateTime(dr["LastGet"]);
                    string tran_id = MethodLibrary.SafeString(dr["transaction_id"]);
                    //string tenToKhai = MethodLibrary.SafeString(dr["TenToKhai"]);
                    string signerPath = MethodLibrary.SafeString(dr["SignerPath"]);
                    string url = $"https://rmgateway.vnptit.vn/sca/sp769/v1/signatures/sign/{tran_id}/status";
                    try
                    {

                        ResStatus res = _smartCAService.GetStatus(url);
                        // neu ko tra ve res cho chay lay tiep cac ket qua khac
                        if (res == null) continue;

                        if (res.message == "EXPIRED")
                        {
                            //khi to khai da het han update trang thai
                            UpdateStatusHoSo(GuidHS, TrangThaiHoso.HetHan, "File [BHXHDienTu.xml] đã hết hạn để ký xác nhận");
                            continue;
                        }
                        IHashSigner signer = null;
                        bool isSigned = false;
                        string pathSaved = Path.Combine(SignedTempFolder, GuidHS, "BHXHDienTu.xml");

                        signer = RestoreSignerXML(signerPath);
                        if (signer == null)
                        {
                            UpdateStatusHoSo(GuidHS, TrangThaiHoso.KyLoi, "Không tạo được signer");
                            continue;
                        }
                        isSigned = GetResult_Xml(signer, res.data, pathSaved);
                        if (!isSigned)
                        {
                            //voi nhung truong hop ko loi ma chua lay dc ket qua thi se ko update trang thai ma chi update LastGet
                            //voi nhung ban ghi ma tinh tu luc cuoi lay kq den hien tai ma chua dc 10s thi se bo qua
                            ListHSChuaCoKQ.Add(idHS);
                            continue;
                        }
                        //update thanh trang thai da ky
                        UpdateStatusHoSo(GuidHS, TrangThaiHoso.DaKy, "", pathSaved);
                    }
                    catch (DatabaseInteractException ex)
                    {
                        Utilities.logger.ErrorLog(ex, "Lỗi tương tác database");
                        continue;
                    }
                    catch (Exception ex)
                    {
                        Utilities.logger.ErrorLog(ex, $"Lỗi khi ký file BHXHDienTu.xml: {GuidHS}");
                        continue;
                    }
                }
                UpdateLastGetHoSo(ListHSChuaCoKQ);
            }
            catch (DatabaseInteractException ex)
            {
                Utilities.logger.ErrorLog(ex, "GetResultToKhai_VNPT");
            }
        }
        #endregion
    }
}
