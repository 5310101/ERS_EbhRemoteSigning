using ERS_Domain;
using ERS_Domain.CAService;
using ERS_Domain.clsUtilities;
using ERS_Domain.Model;
using ERS_Domain.Response;
using Newtonsoft.Json;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using VnptHashSignatures.Common;
using VnptHashSignatures.Interface;
using VnptHashSignatures.Office;
using VnptHashSignatures.Xml;

namespace ws_GetResult_RemoteSigning
{
    public partial class ServiceGetResult_VNPT : ServiceBase
    {
        private DbService _dbService;
        private SmartCAService _smartCAService;
        private Timer _mainTimer;
        //thoi gian chay tu dong cua timer co the dieu chinh, mac dinh la 0.1s
        private int _timeInterval = int.Parse(ConfigurationManager.AppSettings["TIME_INTERVAL"]);
        //biến quy định 1 lần timer tick sẽ xử lý bao nhiêu file mặc định là 10
        private int FileCount = int.Parse(ConfigurationManager.AppSettings["FILE_COUNT"]);
        private string HoSoSavedPath = ConfigurationManager.AppSettings["HOSOTEMP_SAVE"];

        public ServiceGetResult_VNPT()
        {
            InitializeComponent();
            _dbService = new DbService();
            _smartCAService = new SmartCAService(Utilities.glbVar.ConfigRequest);

        }

        private void MainTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            _mainTimer.Enabled = false;
            try
            {

            }
            catch (Exception ex)
            {
                Utilities.logger.ErrorLog(ex, "Timer_Elapsed");
            }
            finally
            {
                _mainTimer.Enabled = true;
            }
        }

        protected override void OnStart(string[] args)
        {
            Utilities.logger.InfoLog("OnStart", "Service started");
            try
            {
                _mainTimer = new Timer();
                _mainTimer.Interval = _timeInterval;
                _mainTimer.AutoReset = true;
                _mainTimer.Elapsed += MainTimer_Elapsed;
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

        private void GetResultToKhai_VNPT()
        {
            try
            {
                //lay cac ban ghi to khai da ky hash
                string TSQL = $"SELECT TOP {FileCount} FROM ToKhai_VNPT WHERE TrangThai=1 ORDER BY NgayGui";
                DataTable dt = _dbService.GetDataTable(TSQL);
                if (dt.Rows.Count == 0)
                {
                    return;
                }
                foreach (DataRow dr in dt.Rows)
                {
                    string GuidHS = MethodLibrary.SafeString(dr["GuidHS"]);
                    string tran_id = MethodLibrary.SafeString(dr["transaction_id"]);
                    string tenToKhai = MethodLibrary.SafeString(dr["TenToKhai"]);
                    string signerPath = MethodLibrary.SafeString(dr["SignerPath"]);
                    string url = $"https://rmgateway.vnptit.vn/sca/sp769/v1/signatures/sign/{tran_id}/status";
                    ResStatus res = _smartCAService.GetStatus(url);
                    // neu ko tra ve res cho chay lay tiep cac ket qua khac
                    if (res == null) continue;

                    if (res.message == "EXPIRED")
                    {
                        //khi to khai da het han update trang thai
                        bool isUpdated = UpdateStatusToKhai(MethodLibrary.SafeNumber<int>(dr["id"]), TrangThaiFile.HetHan, "Tờ khai đã hết hạn để ký xác nhận");
                        if (!isUpdated)
                        {
                            //Co loi co so du lieu thi return luon
                            return;
                        }
                        continue;
                    }
                    IHashSigner signer = null;
                    bool isSigned = false;
                    string pathSaved = Path.Combine(HoSoSavedPath, GuidHS, tenToKhai);
                    switch (Path.GetExtension(tenToKhai))
                    {
                        case ".pdf":
                            isSigned = GetResult_PDF(res.data, pathSaved);
                            break;
                        case ".xml":
                            signer = RestoreSignerXML(signerPath);
                            if (signer == null)
                            {
                                dr["TrangThai"] = (int)TrangThaiFile.KyLoi;
                                dr["ErrMsg"] = "Không tạo được signer";
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
                                dr["TrangThai"] = (int)TrangThaiFile.KyLoi;
                                dr["ErrMsg"] = "Không tạo được signer";
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
                    dr["TrangThai"] = (int)TrangThaiFile.DaKy;
                    dr["FilePath"] = pathSaved;
                }
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

        private bool UpdateStatusToKhai(int id, TrangThaiFile TrangThai, string errMsg = "", string PathFile = "")
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
                throw new Exception();
            }
        }
    }
}
