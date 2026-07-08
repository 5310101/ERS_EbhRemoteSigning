using ERS_Domain;
using ERS_Domain.CAService;
using ERS_Domain.clsUtilities;
using ERS_Domain.CustomSigner;
using ERS_Domain.Dtos;
using ERS_Domain.Exceptions;
using ERS_Domain.Model;
using ERS_Domain.Response;
using Microsoft.SqlServer.Server;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Principal;
using VnptHashSignatures.Common;
using VnptHashSignatures.Interface;
using VnptHashSignatures.Office;
using VnptHashSignatures.Pdf;
using ws_GetResult_RemoteSigning.Cache;

namespace ws_GetResult_RemoteSigning.Utils
{
    public class SigningService
    {
        private readonly DbService _dbService;
        private readonly SmartCAService _smartCAService;
        //private static List<TSDHashSigner> listSigner = new List<TSDHashSigner>();


        //biến quy định 1 lần timer tick sẽ xử lý bao nhiêu file mặc định là 10
        private int FileCount = int.Parse(ConfigurationManager.AppSettings["FILE_COUNT"]);
        //biến quy định 1 lần timer tick sẽ xử lý bao nhiêu file mặc định là 3
        private int HSSignCount = int.Parse(ConfigurationManager.AppSettings["SIGNHS_COUNT"]);
        //biến quy định 1 lần timer tick sẽ xử lý bao nhiêu file mặc định là 10
        private int HoSoCount = int.Parse(ConfigurationManager.AppSettings["HOSO_COUNT"]);

        private readonly string SignedTempFolder = ConfigurationManager.AppSettings["HOSO_TEMP_FOLDER"];

        public SigningService()
        {
            _dbService = new DbService();
            _smartCAService = new SmartCAService(Utilities.glbVar.ConfigRequest);
        }

        #region ham lay ket qua ky tu server VNPT
        public void GetResultToKhai_VNPT(HoSoMessage hs)
        {

            foreach (var toKhai in hs.toKhais)
            {
                string GuidHS = hs.guid;
                int id = toKhai.Id;
                string tran_id = toKhai.TransactionId;
                string tenToKhai = toKhai.TenToKhai;
                string filePath = toKhai.FilePath;
                //lay signer theo transaction_id
                string url = $"{VNPT_URI.uriGetResult}/{tran_id}/status";


                ResStatus res = _smartCAService.GetStatus(url);
                if (res == null)
                {
                    throw new Exception("Cannot get result");
                }

                if (res.message == "PENDING")
                {
                    //van chua lay ket qua thi continue sau lay tiep
                    throw new NotSigningFromUserException("Waiting for user to sign");
                }

                if (res.message == "EXPIRED")
                {
                    throw new SigningExpiredException(filePath, "The file's signing time has expired");
                }

                if (res.message == "REJECTED")
                {
                    //khi to khai da het han update trang thai
                    throw new SigningRejectedException(filePath, "The file has been rejected");
                }

                //TSDHashSigner TSDSigner = listSigner.FirstOrDefault(s => s.Id == signerId);
                //lay signner de them cks vao file
                IHashSigner signer = SigningCache.GetSignerCache<IHashSigner>(tran_id);
                //file pdf ky bang signer profile ko can luu tru signer
                if (signer == null && Path.GetExtension(tenToKhai) != ".pdf")
                {
                    throw new Exception("Cannot find signer");
                }
                bool isSigned = false;
                //thu muc duong dan save file to khai sau khi ky

                switch (Path.GetExtension(tenToKhai))
                {
                    case ".pdf":
                        isSigned = GetResult_PDF(res.data, filePath);
                        break;
                    case ".xml":
                        isSigned = GetResult_Xml(signer, res.data, filePath);
                        break;
                    case ".docx":
                    case ".xlsx":
                        isSigned = GetResult_Office(signer, res.data, filePath);
                        break;
                    default:
                        //khi tiep nhan file o webservice la da kiem tra kieu file
                        throw new Exception("Không hỗ trợ kiểu file ký");
                }
                if (!isSigned)
                {
                    //khong them dc chu ky so thi retry
                    throw new Exception("Cannot add signature to file");
                }
                //update thanh trang thai da ky va xoa signer ra khoi bo nho
                SigningCache.RemoveSigner(tran_id);
                UpdateStatusToKhai(id, TrangThaiFile.DaKy, "", filePath);
            }
        }

        private SignerProfile RestoreSigner(string signerPath)
        {
            try
            {
                SignerProfile signerProfile = MethodLibrary.ImportSigner(signerPath);
                if (signerProfile == null)
                {
                    return null;
                }
                return signerProfile;
            }
            catch (Exception ex)
            {
                Utilities.logger.ErrorLog(ex, "RestoreSigner");
                return null;
            }
        }
        public bool GetResult_PDF(DataTransaction transactionStatus, string PDFSignedPath)
        {
            var isConfirm = false;
            var datasigned = "";
            var mapping = "";
            string tempFolder = Path.GetTempPath();
            if (transactionStatus?.signatures != null)
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
                throw new Exception($"Signature not match {transactionStatus.transaction_id}");
            }

            byte[] signed = signer1.Sign(signerProfileNew, datasigned);
            try
            {
                File.WriteAllBytes(PDFSignedPath, signed);
            }
            catch (UnauthorizedAccessException ex)
            {
                Utilities.logger.ErrorLog(ex, "UnauthorizedAccessException");
                throw new Exception($"Cannot access to file {PDFSignedPath}");
            }
            return true;
        }
        public bool GetResult_Office(IHashSigner signer, DataTransaction transactionStatus, string officeSignedPath)
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
            byte[] signed = ((OfficeHashSigner)signer).Sign(datasigned);
            try
            {
                File.WriteAllBytes(officeSignedPath, signed);
            }
            catch (UnauthorizedAccessException ex)
            {
                Utilities.logger.ErrorLog(ex, "UnauthorizedAccessException");
                throw new Exception($"Cannot access to file {officeSignedPath}");
            }
            return true;
        }
        public bool GetResult_Xml(IHashSigner signer, DataTransaction transactionStatus, string xmlSignedPath)
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
            //Dung CustomXmlSigner de debug dc
            //IHashSigner signerNew = new CustomXmlSigner();

            if (!signer.CheckHashSignature(datasigned))
            {
                Utilities.logger.ErrorLog("Cannot valid signature", transactionStatus.transaction_id);
                throw new Exception($"Cannot valid signature {transactionStatus.transaction_id}");
            }

            byte[] signed = ((CustomXmlSigner)signer).Sign(datasigned);
            try
            {
                File.WriteAllBytes(xmlSignedPath, signed);
            }
            catch (UnauthorizedAccessException ex)
            {
                Utilities.logger.ErrorLog(ex, "UnauthorizedAccessException");
                throw new Exception($"Cannot access to file {xmlSignedPath}");
            }
            return true;
        }
        #endregion

        #region ham ky to khai vnpt
        public void SignToKhai_VNPT(HoSoMessage hoso)
        {
            foreach (var toKhai in hoso.toKhais)
            {
                string GuidHS = hoso.guid;
                string uid = hoso.uid;
                int id = toKhai.Id;
                string serialNumber = hoso.serialNumber;
                string tenToKhai = toKhai.TenToKhai;
                FileType type = (FileType)toKhai.LoaiFile;
                string FilePath = toKhai.FilePath;
                DataSign dataSign = null;
                //SignedHashInfo signedHashInfo = new SignedHashInfo();
                UserCertificate cert = _smartCAService.GetAccountCert(VNPT_URI.uriGetCert, uid, serialNumber);
                if (cert == null)
                {
                    //khong tim dc cks thi se retry
                    UpdateStatusToKhai(id, TrangThaiFile.KyLoi, $"Cannot find certificate of {uid}");
                    throw new Exception($"Cannot find certificate of {uid}");
                }
                byte[] Data = null;
                if (File.Exists(FilePath))
                {
                    try
                    {
                        Data = File.ReadAllBytes(FilePath);
                    }
                    catch (Exception ex)
                    {
                        throw new FileErrorException(toKhai.Id, FilePath, $"Read file {FilePath} error, {ex.Message}", ex);
                    }

                    if (Data.Length == 0)
                    {
                        throw new FileErrorException(toKhai.Id, FilePath, $"File {FilePath} error");
                    }
                }
                else
                {
                    throw new FileErrorException(toKhai.Id, FilePath, $"File not found: {FilePath}");
                }   

                IHashSigner signer = null;
                string errMessage = "";
                switch (type)
                {
                    case FileType.PDF:
                        dataSign = SignSmartCAPDF(cert, Data, uid, ref errMessage);
                        break;
                    case FileType.XML:
                        dataSign = SignSmartCAXML(cert, Data, uid, ref signer, ref errMessage);
                        break;
                    case FileType.OFFICE:
                        dataSign = SignSmartCAOFFICE(cert, Data, uid, ref signer, ref errMessage);
                        break;
                    default:
                        return;
                }
                //1 file ky loi thi them vao list guid loi
                if (dataSign == null)
                {
                    //update to khai ky loi
                    UpdateStatusToKhai(id, TrangThaiFile.KyLoi, errMessage);
                    //signhash lỗi thì lần sau cũng ko ký dc nên update luôn trạng thái hồ sơ lỗi
                    throw new SignHashException(GuidHS, id, errMessage);
                }
                //signedHashInfo.SignData = dataSign;
                if (signer == null)
                {
                    throw new Exception($"Sign hash error: {errMessage}");
                }
                toKhai.TransactionId = dataSign.transaction_id;
                toKhai.TransCode = dataSign.tran_code;
                SigningCache.SetSigningCache(dataSign.transaction_id, signer);
                //update trang thai to khai
                UpdateStatusToKhai(id, TrangThaiFile.DaKyHash, "", FilePath, dataSign.transaction_id, dataSign.transaction_id, dataSign.tran_code);
            }
        }

        public UserCertificate GetCertificate(string uid, string serialNumber)
        {
            UserCertificate cert = _smartCAService.GetAccountCert(VNPT_URI.uriGetCert, uid, serialNumber);
            return cert;
        }
        #endregion

        #region cac ham ky remote
        public DataSign SignSmartCAPDF(UserCertificate userCert, byte[] pdfUnsign, string uid, ref string errMessage)
        {
            try
            {
                //if (pdfUnsign == null) { return null; }
                IHashSigner signer = HashSignerFactory.GenerateSigner(pdfUnsign, userCert.cert_data, null, HashSignerFactory.PDF);
                signer.SetHashAlgorithm(MessageDigestAlgorithm.SHA256);

                #region Optional -----------------------------------
                // Property: Lý do ký số
                ((PdfHashSigner)signer).SetReason("Xác nhận tài liệu");

                ((PdfHashSigner)signer).SetRenderingMode(PdfHashSigner.RenderMode.TEXT_ONLY);
                // Nội dung text trên chữ ký (OPTIONAL)
                string subject = userCert.cert_subject;
                string nguoiKy = subject.GetSubjectValue("CN=");
                string noiKy = subject.GetSubjectValue("ST=");

                ((PdfHashSigner)signer).SetLayer2Text($"Ngày ký: {DateTime.Now.Date} \n Người ký: {nguoiKy} \n Nơi ký: {noiKy}");
                // Fontsize cho text trên chữ ký (OPTIONAL/DEFAULT = 10)
                ((PdfHashSigner)signer).SetFontSize(10);
                //((PdfHashSigner)signer).SetLayer2Text("yahooooooooooooooooooooooooooo");
                // Màu text trên chữ ký (OPTIONAL/DEFAULT=000000)
                ((PdfHashSigner)signer).SetFontColor("0000ff");
                // Kiểu chữ trên chữ ký
                ((PdfHashSigner)signer).SetFontStyle(PdfHashSigner.FontStyle.Normal);
                // Font chữ trên chữ ký
                ((PdfHashSigner)signer).SetFontName(PdfHashSigner.FontName.Arial);

                // Hiển thị ảnh chữ ký tại nhiều vị trí trên tài liệu
                ((PdfHashSigner)signer).AddSignatureView(new PdfSignatureView
                {
                    Rectangle = "10,10,250,100",

                    Page = 1
                });


                #endregion -----------------------------------------            

                var profile = signer.GetSignerProfile();

                var profileJson = JsonConvert.SerializeObject(profile);

                var hashValue = Convert.ToBase64String(profile.SecondHashBytes);
                if (string.IsNullOrEmpty(hashValue))
                {
                    throw new Exception("Hash file error");
                }

                var data_to_be_sign = BitConverter.ToString(Convert.FromBase64String(hashValue)).Replace("-", "").ToLower();

                string tempFolder = Path.GetTempPath();
                File.AppendAllText(tempFolder + data_to_be_sign + ".txt", profileJson);

                DataSign dataSign = _smartCAService.Sign(VNPT_URI.uriSign, data_to_be_sign, userCert.serial_number, uid, "pdf");

                return dataSign;
            }
            catch (Exception ex)
            {
                Utilities.logger.ErrorLog(ex, "SignSmartCAPDF", userCert.cert_subject);
                errMessage = ex.Message;
                return null;
            }
        }

        public DataSign SignSmartCAXML(UserCertificate userCert, byte[] xmlUnsign, string uid, ref IHashSigner signer, ref string errMesage, string nodeKy = "")
        {
            try
            {
                String certBase64 = userCert.cert_data;
                //signer = HashSignerFactory.GenerateSigner(xmlUnsign, certBase64, null, HashSignerFactory.XML);
                signer = MethodLibrary.GenerateCustomSigner(xmlUnsign, certBase64);
                signer.SetHashAlgorithm(MessageDigestAlgorithm.SHA256);


                //Set ID cho thẻ ssignature
                //((XmlHashSigner)signer).SetSignatureID(Guid.NewGuid().ToString());
                ((CustomXmlSigner)signer).SetSignatureID("sigid");

                //Set reference đến id
                //((XmlHashSigner)signers).SetReferenceId("#SigningData");

                //Set thời gian ký
                ((CustomXmlSigner)signer).SetSigningTime(DateTime.Now, "proid");

                //đường dẫn dẫn đến thẻ chứa chữ ký 
                if (nodeKy == "")
                {
                    nodeKy = "//Cky";
                }
                ((CustomXmlSigner)signer).SetParentNodePath(nodeKy);

                var hashValue = signer.GetSecondHashAsBase64();
                if (string.IsNullOrEmpty(hashValue))
                {
                    throw new Exception("Hash file error");
                }
                //signerProfile = signer.GetSignerProfile();
                //var hashValue = Convert.ToBase64String(signerProfile.SecondHashBytes);
                var data_to_be_sign = BitConverter.ToString(Convert.FromBase64String(hashValue)).Replace("-", "").ToLower();

                DataSign dataSign = _smartCAService.Sign(VNPT_URI.uriSign, data_to_be_sign, userCert.serial_number, uid, "xml");

                return dataSign;

            }
            catch (Exception ex)
            {
                Utilities.logger.ErrorLog(ex, "SignSmartCAXML", userCert.cert_subject);
                signer = null;
                errMesage = ex.Message;
                return null;
            }
        }

        public DataSign SignSmartCAOFFICE(UserCertificate userCert, byte[] officeUnsign, string uid, ref IHashSigner signer, ref string errMessage)
        {
            try
            {
                String certBase64 = userCert.cert_data;
                signer = HashSignerFactory.GenerateSigner(officeUnsign, certBase64, null, HashSignerFactory.OFFICE);
                signer.SetHashAlgorithm(MessageDigestAlgorithm.SHA256);

                var hashValue = signer.GetSecondHashAsBase64();
                if (string.IsNullOrEmpty(hashValue))
                {
                    throw new Exception("Hash file error");
                }
                //signerProfile = signer.GetSignerProfile();
                //var hashValue = Convert.ToBase64String(signerProfile.SecondHashBytes);

                var data_to_be_sign = BitConverter.ToString(Convert.FromBase64String(hashValue)).Replace("-", "").ToLower();

                DataSign dataSign = _smartCAService.Sign(VNPT_URI.uriSign, data_to_be_sign, userCert.serial_number, uid, "office");
                return dataSign;
            }
            catch (Exception ex)
            {
                Utilities.logger.ErrorLog(ex, "SignSmartCAOFFICE", userCert.cert_subject);
                signer = null;
                errMessage = ex.Message;
                return null;
            }
        }
        #endregion

        #region database interact
        public void UpdateStatusToKhai(int id, TrangThaiFile TrangThai, string errMsg = "", string FilePath = "", string SignerId = "", string transaction_id = "", string tran_code = "")
        {
            bool result = _dbService.ExecQuery("UPDATE ToKhai_RS SET TrangThai=@TrangThai, ErrMsg=@ErrMsg, FilePath=@FilePath, SignerId=@SignerId, transaction_id=@transaction_id, tran_code=@tran_code, LastGet=@LastGet WHERE id=@id", "", new SqlParameter[]
                {
                new SqlParameter("@TrangThai", (int)TrangThai),
                new SqlParameter("@ErrMsg", errMsg.SafeString()),
                new SqlParameter("@FilePath", FilePath.SafeString()),
                new SqlParameter("@SignerId", SignerId.SafeString()),
                new SqlParameter("@transaction_id", transaction_id.SafeString()),
                new SqlParameter("@tran_code", tran_code.SafeString()),
                new SqlParameter("@LastGet", DateTime.Now),
                new SqlParameter("@id", id),
                });
            if (!result)
            {
                throw new DatabaseInteractException($"Có lỗi khi update trạng thái tờ khai: {id}");
            }
        }

        public void UpdateLastGetToKhai(List<int> listToKhaiId)
        {
            if (listToKhaiId.Count > 0)
            {
                string strListId = string.Join(",", listToKhaiId);
                string TSQL = $"UPDATE ToKhai_RS SET LastGet=@LastGet WHERE id IN ({strListId})";
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

        public void UpdateStatusHoSo(string GuidHS, TrangThaiHoso TrangThai, string errMsg = "", string signerId = "", string PathFile = "", string transaction_id = "", string tran_code = "")
        {
            bool result = _dbService.ExecQuery("UPDATE HoSo_RS SET TrangThai=@TrangThai, ErrMsg=@ErrMsg, SignerId=@SignerId, FilePath=@FilePath, transaction_id=@transaction_id, tran_code=@tran_code, LastGet=@LastGet WHERE Guid=@Guid", "", new SqlParameter[]
                {
                new SqlParameter("@TrangThai", (int)TrangThai),
                new SqlParameter("@ErrMsg", errMsg),
                new SqlParameter("@SignerId", signerId),
                new SqlParameter("@FilePath", PathFile),
                new SqlParameter("@transaction_id", transaction_id),
                new SqlParameter("@tran_code", tran_code),
                new SqlParameter("@LastGet", DateTime.Now),
                new SqlParameter("@Guid", GuidHS),
                });
            if (!result)
            {
                throw new DatabaseInteractException($"Có lỗi khi update trạng thái hồ sơ: {GuidHS}");
            }
        }

        //Update ho so loi va xoa folder tam
        public void UpdateHoSo_SignFailed(List<string> listGuid)
        {
            foreach (string guid in listGuid)
            {
                bool isUpdated = _dbService.ExecQuery("UPDATE HoSo_RS SET TrangThai=0, ErrMsg=@ErrMsg WHERE Guid=@Guid", "", new SqlParameter[]
                {
                    new SqlParameter("@Guid",guid),
                    new SqlParameter("@ErrMsg","Sign Error")
                });
                string tempHS = Path.Combine(SignedTempFolder, guid);
                if (isUpdated && Directory.Exists(tempHS))
                {
                    Directory.Delete(tempHS, true);
                }
            }
        }

        public void UpdateHoSo_Expired(List<string> listGuid)
        {
            foreach (string guid in listGuid)
            {
                bool isUpdated = _dbService.ExecQuery("UPDATE HoSo_RS SET TrangThai=3, ErrMsg=@ErrMsg WHERE Guid=@Guid", "", new SqlParameter[]
                {
                    new SqlParameter("@Guid",guid),
                    new SqlParameter("@ErrMsg","Expired")
                });
                string tempHS = Path.Combine(SignedTempFolder, guid);
                if (isUpdated && Directory.Exists(tempHS))
                {
                    Directory.Delete(tempHS, true);
                }
            }
        }

        public void UpdateLastGetHoSo(List<int> ListIdHS)
        {
            if (ListIdHS == null || ListIdHS.Count == 0)
            {
                return;
            }
            string strListId = string.Join(",", ListIdHS);
            string TSQL = $"UPDATE HoSo_RS SET LastGet=@LastGet WHERE id IN ({strListId})";
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

        public void CreateFileHoSoDK_LanDau(HoSoMessage hs, string pathFileHSDKLD, DataTable dtHSDK)
        {
            List<FileToKhai> listTK = new List<FileToKhai>();
            foreach (ToKhai signed in hs.toKhais)
            {
                byte[] tkDaKy = File.ReadAllBytes(signed.FilePath);
                string base64Data = Convert.ToBase64String(tkDaKy);
                string tenFile = MethodLibrary.SafeString(signed.TenToKhai);

                FileToKhai tk = new FileToKhai()
                {
                    MaToKhai = MethodLibrary.GetMaTK(tenFile),
                    MoTaToKhai = signed.MoTaToKhai,
                    TenFile = tenFile,
                    LoaiFile = Path.GetExtension(tenFile),
                    DoDaiFile = base64Data.Length,
                    NoiDungFile = base64Data,
                };
                listTK.Add(tk);
            }

            if (dtHSDK.Rows.Count == 0) return;
            DataRow row = dtHSDK.Rows[0];
            HosoDKLanDauObjSerialize hsdk = new HosoDKLanDauObjSerialize()
            {
                NoiDung = new NoiDungDK()
                {
                    TenCoQuan = row["TenCoQuan"].SafeString(),
                    MaCoQuan = row["MaCoQuan"].SafeString(),
                    LoaiDoiTuong = row["LoaiDoiTuong"].SafeString(),
                    TenDoiTuong = row["TenDoiTuong"].SafeString(),
                    MaSoThue = row["MaSoThue"].SafeString(),
                    DienThoai = row["DienThoai"].SafeString(),
                    Email = row["Email"].SafeString(),
                    NguoiLienHe = row["NguoiLienHe"].SafeString(),
                    DiaChi = row["DiaChi"].SafeString(),
                    DiaChiLienHe = row["DiaChiLienHe"].SafeString(),
                    DienThoaiLienHe = row["DienThoaiLienHe"].SafeString(),
                    NgayLap = row["NgayLap"].SafeDateTime().ToString("dd/MM/yyyy"),
                    NgayDangKy = row["NgayDangKy"].SafeDateTime().ToString("dd/MM/yyyy"),
                    PTNhanKetQua = row["PTNhanKetQua"].SafeString(),
                    ToKhais = new ToKhais()
                    {
                        FileToKhai = listTK.ToArray()
                    },
                },
            };
            hs.filePathHS = pathFileHSDKLD;
            MethodLibrary.SerializeToFile(hsdk, pathFileHSDKLD);
        }

        public void CreateBHXHDienTu(HoSoMessage hs, string pathFolderHoSo)
        {
            string GuidHS = hs.guid;
            var tokhais = hs.toKhais;
            List<FileToKhai> listTK = new List<FileToKhai>();
            foreach (var tokhai in hs.toKhais)
            {
                byte[] tkDaKy = File.ReadAllBytes(tokhai.FilePath);
                string base64Data = Convert.ToBase64String(tkDaKy);
                string tenFile = tokhai.TenToKhai;

                FileToKhai tk = new FileToKhai()
                {
                    MaToKhai = MethodLibrary.GetMaTK(tenFile),
                    MoTaToKhai = tokhai.MoTaToKhai,
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
                MaThuTuc = hs.maNV.GetMaThuTuc(),
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
            string pathBHXHDT = Path.Combine(pathFolderHoSo, "BHXHDienTu.xml");
            hs.filePathHS = pathBHXHDT;
            if (!MethodLibrary.SerializeToFile(hoso, pathBHXHDT))
            {
                throw new Exception($"Cannot serialize file BHXHDienTu.xml for {GuidHS}");
            }
        }

        public void SignHoSoBHXH(HoSoMessage hs)
        {
            string errMessage = "";

            //lay cert
            UserCertificate userCert = _smartCAService.GetAccountCert(VNPT_URI.uriGetCert, hs.uid, hs.serialNumber);
            if (userCert == null)
            {
                throw new Exception("Cannot find certificate");
            }
            //string PathTempHoSo = Path.Combine(SignedTempFolder, hs.guid);
            //string pathBHXHDt = "";
            //if (hs.typeDK == TypeHS.HSNV)
            //{
            //    pathBHXHDt = Path.Combine(PathTempHoSo, "BHXHDienTu.xml");
            //}
            //else if (hs.typeDK == TypeHS.HSDKLanDau || hs.typeDK == TypeHS.HSDK )
            //{
            //    pathBHXHDt = Path.Combine(PathTempHoSo, $"{HSDKName}.xml");
            //}

            IHashSigner signer = null;

            byte[] DataBHXHDt = null;
            if (File.Exists(hs.filePathHS))
            {
                try
                {
                    DataBHXHDt = File.ReadAllBytes(hs.filePathHS);
                }
                catch (Exception ex)
                {
                    throw new FileErrorException(-1, hs.filePathHS, $"Cannot read file in path {hs.filePathHS}: {ex.Message}") ;
                }
               if(DataBHXHDt.Length == 0)
               {
                   throw new FileErrorException(-1, hs.filePathHS, $"File {hs.filePathHS} is empty");
               }
            }
            else
            {
                throw new FileErrorException(-1, hs.filePathHS, $"File not found: {hs.filePathHS}");
            }

            DataSign dataSign = SignSmartCAXML(userCert, DataBHXHDt, hs.guid, ref signer, ref errMessage, "/Hoso/CKy_Dvi");

            if (dataSign == null || signer == null)
            {
                //update trang thai ky loi
                throw new Exception($"Sign hash error: {errMessage}");
            }

            SigningCache.SetSigningCache(dataSign.transaction_id, signer);
            hs.transactionId = dataSign.transaction_id;
            hs.transCode = dataSign.tran_code;
            UpdateStatusHoSo(hs.guid, TrangThaiHoso.DaKyHash, "", dataSign.transaction_id, "", dataSign.transaction_id, dataSign.tran_code);
        }
        public void GetResultHoSo_VNPT(HoSoMessage hs)
        {
            string GuidHS = hs.guid;
            string tran_id = hs.transactionId;
            //string tenToKhai = MethodLibrary.SafeString(dr["TenToKhai"]);
            string url = $"{VNPT_URI.uriGetResult}/{tran_id}/status";
            string maNV = hs.maNV;
            ResStatus res = _smartCAService.GetStatus(url);
            if (res == null)
            {
                throw new Exception($"Cannot get status of {hs.guid}-transaction:{tran_id}");
            }

            if (res.message == "PENDING")
            {
                // neu chua lay ket qua do chua ky ben app vnpt 
                throw new NotSigningFromUserException($"Waiting for user to sign {hs.guid}-transaction:{tran_id}");
            }

            if (res.message == "EXPIRED")
            {
                //het han
                throw new SigningExpiredException(hs.filePathHS, $"{hs.guid}-transactionId[{tran_id}]: signing time has expired");
            }
            if (res.message == "REJECTED")
            {
                //tu choi ky
                throw new SigningRejectedException(hs.filePathHS, $"{hs.guid}-transactionId[{tran_id}]: has been rejected");
            }
            bool isSigned = false;

            var signer = SigningCache.GetSignerCache<IHashSigner>(tran_id);

            if (signer == null)
            {
                throw new Exception($"Cannot find signer for {hs.guid}-transaction:{tran_id}");

            }
            isSigned = GetResult_Xml(signer, res.data, hs.filePathHS);
            if (!isSigned)
            {
                throw new Exception($"Cannot add signature to file {hs.filePathHS} for {hs.guid}-transaction:{tran_id}");

            }
            //update thanh trang thai da ky
            UpdateStatusHoSo(GuidHS, TrangThaiHoso.DaKy, "", "", hs.filePathHS);
        }
        #endregion
    }
}


