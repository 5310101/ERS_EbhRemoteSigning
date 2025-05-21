using ERS_Domain.CAService;
using ERS_Domain.clsUtilities;
using ERS_Domain.CustomSigner;
using ERS_Domain.Exceptions;
using ERS_Domain.Model;
using ERS_Domain;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
using System.IO;
using System.Linq;
using VnptHashSignatures.Common;
using VnptHashSignatures.Interface;
using VnptHashSignatures.Office;
using VnptHashSignatures.Pdf;
using ERS_Domain.Response;
using System.Configuration;

namespace ws_GetResult_RemoteSigning.Utils
{
    public class SigningService
    {
        private readonly DbService _dbService;
        private readonly SmartCAService _smartCAService;
        private static List<TSDHashSigner> listSigner = new List<TSDHashSigner>();

        //biến quy định 1 lần timer tick sẽ xử lý bao nhiêu ho so mặc định là 3 ho so chua to khai
        private int _signTK_HSCount = int.Parse(ConfigurationManager.AppSettings["TKHS_COUNT"]);
        //biến quy định 1 lần timer tick sẽ xử lý bao nhiêu file mặc định là 10
        private int FileCount = int.Parse(ConfigurationManager.AppSettings["FILE_COUNT"]);
        //biến quy định 1 lần timer tick sẽ xử lý bao nhiêu file mặc định là 3
        private int HSSignCount = int.Parse(ConfigurationManager.AppSettings["SIGNHS_COUNT"]);
        //biến quy định 1 lần timer tick sẽ xử lý bao nhiêu file mặc định là 10
        private int HoSoCount = int.Parse(ConfigurationManager.AppSettings["HOSO_COUNT"]);
        //biến quy đinh 1 lần timer sẽ xử lý bao nhiêu file HSDK (trừ hồ sơ đăng ký lấy mã đơn vị lần đầu)
        private int _signHSDKCount = int.Parse(ConfigurationManager.AppSettings["HSDK_COUNT"]);

        private readonly string SignedTempFolder = ConfigurationManager.AppSettings["HOSO_TEMP_FOLDER"];

        public SigningService()
        {
            _dbService = new DbService();
            _smartCAService = new SmartCAService(Utilities.glbVar.ConfigRequest);
        }

        #region ham lay ket qua ky tu server VNPT
        public void GetResultToKhai_VNPT()
        {
            List<string> listGuidKoLayDuocKQ = new List<string>();
            List<string> listHetHan = new List<string>();
            //lay cac ban ghi to khai da ky hash
            //sau khi lay ket qua thi 10s sau ms lay lai ket qua neu ko thanh cong
            string TSQL = $"SELECT TOP {FileCount} * FROM ToKhai_VNPT WITH (NOLOCK) WHERE TrangThai=1 AND LastGet <= DATEADD(SECOND,-10,GETDATE()) ORDER BY NgayGui";
            try
            {
                DataTable dt = _dbService.GetDataTable(TSQL);
                if (dt is null || dt.Rows.Count == 0)
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
                    string filePath = MethodLibrary.SafeString(dr["FilePath"]);
                    string signerId = MethodLibrary.SafeString(dr["SignerId"]);
                    string url = $"{VNPT_URI.uriGetResult}/{tran_id}/status";
                    try
                    {

                        ResStatus res = _smartCAService.GetStatus(url);
                        if (res == null)
                        {
                            if (!listGuidKoLayDuocKQ.Contains(GuidHS))
                            {
                                listGuidKoLayDuocKQ.Add(GuidHS);
                            }
                        }

                        if(res.message == "PENDING")
                        {
                            //van chua lay ket qua thi continue sau lay tiep
                            continue;
                        }

                        if (res.message == "EXPIRED")
                        {
                            //khi to khai da het han update trang thai
                            UpdateStatusToKhai(id, TrangThaiFile.HetHan, "The file's signing time has expired");
                            if (!listHetHan.Contains(GuidHS))
                            {
                                listHetHan.Add(GuidHS);
                            }
                            continue;
                        }

                        if (res.message == "REJECTED")
                        {
                            //khi to khai da het han update trang thai
                            UpdateStatusToKhai(id, TrangThaiFile.KyLoi, "The file has been rejected");
                            if (!listGuidKoLayDuocKQ.Contains(GuidHS))
                            {
                                listGuidKoLayDuocKQ.Add(GuidHS);
                            }
                            continue;
                        }

                        TSDHashSigner TSDSigner = listSigner.FirstOrDefault(s => s.Id == signerId);
                        //file pdf ky bang signer profile ko can luu tru signer
                        if (TSDSigner == null && Path.GetExtension(tenToKhai) != ".pdf")
                        {
                            UpdateStatusToKhai(id, TrangThaiFile.KyLoi, "Cannot find signer");
                            continue;
                        }
                        IHashSigner signer = null;
                        bool isSigned = false;
                        //thu muc duong dan save file to khai sau khi ky

                        switch (Path.GetExtension(tenToKhai))
                        {
                            case ".pdf":
                                isSigned = GetResult_PDF(res.data, filePath);
                                break;
                            case ".xml":
                                signer = TSDSigner.Signer;
                                isSigned = GetResult_Xml(signer, res.data, filePath);
                                break;
                            case ".docx":
                            case ".xlsx":
                                signer = TSDSigner.Signer;
                                isSigned = GetResult_Office(signer, res.data, filePath);
                                break;
                            default:
                                //khi tiep nhan file o webservice la da kiem tra kieu file
                                throw new Exception("Không hỗ trợ kiểu file ký");
                        }
                        if (!isSigned)
                        {
                            if (!listGuidKoLayDuocKQ.Contains(GuidHS))
                            {
                                listGuidKoLayDuocKQ.Add(GuidHS);
                            }
                            continue;
                        }
                        //update thanh trang thai da ky va xoa signer ra khoi bo nho
                        listSigner.Remove(TSDSigner);
                        UpdateStatusToKhai(id, TrangThaiFile.DaKy, "", filePath);
                    }
                    catch (DatabaseInteractException ex)
                    {
                        Utilities.logger.ErrorLog(ex, "Lỗi tương tác database");
                        continue;
                    }
                    catch (Exception ex)
                    {
                        Utilities.logger.ErrorLog(ex, $"Lỗi khi lấy kết quả file {GuidHS}[{id}]");
                        UpdateStatusToKhai(id, TrangThaiFile.KyLoi, ex.Message, filePath, signerId, tran_id);
                        if (!listGuidKoLayDuocKQ.Contains(GuidHS))
                        {
                            listGuidKoLayDuocKQ.Add(GuidHS);
                        }
                        continue;
                    }
                }
                UpdateHoSo_SignFailed(listGuidKoLayDuocKQ);
                UpdateHoSo_Expired(listHetHan);
            }
            catch (DatabaseInteractException ex)
            {
                Utilities.logger.ErrorLog(ex, "GetResultToKhai_VNPT");
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
        private bool GetResult_PDF(DataTransaction transactionStatus, string PDFSignedPath)
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
        public void SignToKhai_VNPT()
        {
            try
            {
                //select ho so thoa man dieu kien 
                string TSQL = $"WITH TopHoSo AS (SELECT TOP {_signTK_HSCount} Guid FROM HoSo_VNPT WHERE TrangThai = 4 AND typeDK<>1 AND LastGet < DATEADD(SECOND, -10, GETDATE()) ORDER BY NgayGui),ToKhaiChuaKy AS (SELECT GuidHS FROM ToKhai_VNPT WHERE GuidHS IN (SELECT Guid FROM TopHoSo) GROUP BY GuidHS HAVING COUNT(*) = SUM(CASE WHEN TrangThai = 6 THEN 1 ELSE 0 END)) SELECT * FROM ToKhai_VNPT WHERE GuidHS IN (SELECT GuidHS FROM ToKhaiChuaKy);";
                DataTable dt = _dbService.GetDataTable(TSQL);
                if (dt.Rows.Count == 0)
                {
                    return;
                }

                //List<SignedHashInfo> listSignedDTO = new List<SignedHashInfo>();
                List<string> listHoSo_Loi = new List<string>();
                foreach (DataRow row in dt.Rows)
                {
                    string GuidHS = MethodLibrary.SafeString(row["GuidHS"]);
                    //Neu nam trong list ho so loi thi continue 
                    if (listHoSo_Loi.Contains(GuidHS))
                    {
                        continue;
                    }
                    string uid = MethodLibrary.SafeString(row["uid"]);
                    int id = MethodLibrary.SafeNumber<int>(row["id"]);
                    string serialNumber = MethodLibrary.SafeString(row["SerialNumber"]);
                    string tenToKhai = MethodLibrary.SafeString(row["TenToKhai"]);
                    FileType type = (FileType)row["LoaiFile"];
                    string FilePath = MethodLibrary.SafeString(row["FilePath"]);
                    DataSign dataSign = null;
                    //SignedHashInfo signedHashInfo = new SignedHashInfo();
                    UserCertificate cert = _smartCAService.GetAccountCert(VNPT_URI.uriGetCert, uid, serialNumber);
                    if(cert == null)
                    {
                        //khong tim dc cks thi se bao luon ho so loi
                        Utilities.logger.ErrorLog("Cert not found", $"{VNPT_URI.uriGetCert}, {uid},{serialNumber}");
                        UpdateStatusToKhai(id,TrangThaiFile.KyLoi,$"Cannot find certificate of {uid}");
                        listHoSo_Loi.Add(GuidHS);
                        continue;
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
                            Utilities.logger.ErrorLog(ex, "Read File Error", FilePath);
                        }
                    }
                    if (Data == null)
                    {
                        Utilities.logger.ErrorLog("Read File Error", FilePath);
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
                        listHoSo_Loi.Add(GuidHS);
                        continue;
                    }
                    //signedHashInfo.SignData = dataSign;
                    if (signer != null)
                    {
                        TSDHashSigner TSDSigner = new TSDHashSigner()
                        {
                            Id = dataSign.transaction_id,
                            Signer = signer,
                        };
                        listSigner.Add(TSDSigner);
                    }
                    //listSignedDTO.Add(signedHashInfo);
                    //update trang thai to khai
                    try
                    {
                        UpdateStatusToKhai(id, TrangThaiFile.DaKyHash, "", FilePath, dataSign.transaction_id, dataSign.transaction_id, dataSign.tran_code);
                    }
                    catch (DatabaseInteractException ex)
                    {
                        //neu loi db cung coi nhu ho so loi
                        Utilities.logger.ErrorLog(ex, "Lỗi tương tác cơ sở dữ liệu");
                        listHoSo_Loi.Add(GuidHS);
                    }
                }
                //Update trang thai cac ho so loi,cac ho so nay se tra ve ket qua luon 
                UpdateHoSo_SignFailed(listHoSo_Loi);
            }
            catch (Exception ex)
            {
                Utilities.logger.ErrorLog(ex, "SignToKhai_VNPT");
            }
        }
        #endregion

        #region cac ham ky remote
        private DataSign SignSmartCAPDF(UserCertificate userCert, byte[] pdfUnsign, string uid, ref string errMessage)
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

        private DataSign SignSmartCAXML(UserCertificate userCert, byte[] xmlUnsign, string uid, ref IHashSigner signer, ref string errMesage, string nodeKy = "")
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

        private DataSign SignSmartCAOFFICE(UserCertificate userCert, byte[] officeUnsign, string uid, ref IHashSigner signer, ref string errMessage)
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
        private void UpdateStatusToKhai(int id, TrangThaiFile TrangThai, string errMsg = "", string FilePath = "", string SignerId = "", string transaction_id = "", string tran_code = "")
        {
            bool result = _dbService.ExecQuery("UPDATE ToKhai_VNPT SET TrangThai=@TrangThai, ErrMsg=@ErrMsg, FilePath=@FilePath, SignerId=@SignerId, transaction_id=@transaction_id, tran_code=@tran_code, LastGet=@LastGet WHERE id=@id", "", new SqlParameter[]
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

        private void UpdateStatusHoSo(string GuidHS, TrangThaiHoso TrangThai, string errMsg = "", string signerId = "", string PathFile = "", string transaction_id = "", string tran_code = "")
        {
            bool result = _dbService.ExecQuery("UPDATE HoSo_VNPT SET TrangThai=@TrangThai, ErrMsg=@ErrMsg, SignerId=@SignerId, FilePath=@FilePath, transaction_id=@transaction_id, tran_code=@tran_code, LastGet=@LastGet WHERE Guid=@Guid", "", new SqlParameter[]
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
        private void UpdateHoSo_SignFailed(List<string> listGuid)
        {
            foreach (string guid in listGuid)
            {
                bool isUpdated = _dbService.ExecQuery("UPDATE HoSo_VNPT SET TrangThai=0, ErrMsg=@ErrMsg WHERE Guid=@Guid", "", new SqlParameter[]
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

        private void UpdateHoSo_Expired(List<string> listGuid)
        {
            foreach (string guid in listGuid)
            {
                bool isUpdated = _dbService.ExecQuery("UPDATE HoSo_VNPT SET TrangThai=3, ErrMsg=@ErrMsg WHERE Guid=@Guid", "", new SqlParameter[]
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

        private void UpdateLastGetHoSo(List<int> ListIdHS)
        {
            if (ListIdHS == null || ListIdHS.Count == 0)
            {
                return;
            }
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
        public void SignFileBHXHDienTu()
        {
            try
            {
                //select trong bang HoSo_VNPT, chi select cac hoso ma tat ca to khai da dc ky(trang thai =2)
                string TSQL = $"SELECT TOP {HSSignCount} * FROM HoSo_VNPT WITH (NOLOCK) WHERE GUID IN (SELECT GUID FROM HoSo_VNPT A JOIN ToKhai_VNPT B ON A.Guid = B.GuidHS GROUP BY A.Guid HAVING COUNT(*) = SUM(CASE WHEN B.TrangThai = 2 THEN 1 ELSE 0 END)) AND TrangThai=4 ORDER BY NgayGui";
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
                        string maNV = MethodLibrary.SafeString(dr["MaNV"]);
                        int typeDK = MethodLibrary.SafeNumber<int>(dr["typeDK"]);

                        bool isCreated = false; 
                        if (typeDK == 0)
                        {
                            isCreated = CreateBHXHDienTu(dr, pathSaveHS);
                            if (!isCreated)
                            {
                                //ko tao dc file thi continue lan sau tao lai
                                continue;
                            }
                        }
                        else if(typeDK == 2)
                        {
                            string pathFile = Path.Combine(pathSaveHS, $"{maNV}.xml");
                            isCreated = CreateFileHSDK(dr, pathFile);
                            if (!isCreated)
                            {
                                continue;
                            }
                        }
                        bool isSigned = SignHoSoBHXH(uid, GuidHS, serialNumber, typeDK, maNV);
                        if (!isSigned)
                        {
                            Utilities.logger.ErrorLog($"Hồ sơ ký lỗi không lấy kết quả: {GuidHS}", "Hồ sơ ký lỗi");
                            //Xoa thu muc neu ky loi
                            Directory.Delete(pathSaveHS, true);
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

        private bool CreateFileHSDK(DataRow dr, string pathSaveHSDK)
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

                //Load thong tin de tao file HSDK 04DK
                DataTable dtHSDK = _dbService.GetDataTable("SELECT * FROM HSDKLanDau WHERE GuidHS=@Guid", "", new SqlParameter[]
                {
                    new SqlParameter("@Guid", GuidHS)
                });
                if (dtHSDK.Rows.Count == 0) return false;
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

                return MethodLibrary.SerializeToFile(hsdk, pathSaveHSDK);
            }
            catch (Exception ex)
            {
                Utilities.logger.ErrorLog(ex, "CreateFileHSDK");
                return false;
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
                string pathBHXHDT = Path.Combine(pathFolderHoSo, "BHXHDienTu.xml");
                return MethodLibrary.SerializeToFile(hoso, pathBHXHDT);
            }
            catch (Exception ex)
            {
                Utilities.logger.ErrorLog(ex, "CreateBHXHDienTu");
                return false;
            }
        }

        private bool SignHoSoBHXH(string uid, string GuidHS, string serialNumber, int typeDK = 0, string HSDKName = "")
        {
            string errMessage = "";
            try
            {
                //lay cert
                UserCertificate userCert = _smartCAService.GetAccountCert(VNPT_URI.uriGetCert, uid, serialNumber);
                if (userCert == null)
                {
                    Utilities.logger.ErrorLog("Không tìm thấy cert trên server VNPT để ký", "Lỗi khi ký", uid, serialNumber);
                    UpdateStatusHoSo(GuidHS, TrangThaiHoso.KyLoi, $"Cert not found for {uid}");
                    return false;
                }
                string PathTempHoSo = Path.Combine(SignedTempFolder, GuidHS);
                string pathBHXHDt = "";
                if (typeDK == 0)
                {
                    pathBHXHDt = Path.Combine(PathTempHoSo, "BHXHDienTu.xml");
                }
                else if (typeDK == 1 || typeDK == 2)
                {
                    pathBHXHDt = Path.Combine(PathTempHoSo, $"{HSDKName}.xml");
                }
               
                IHashSigner signer = null;
                

                byte[] DataBHXHDt = File.ReadAllBytes(pathBHXHDt);
                DataSign dataSign = SignSmartCAXML(userCert, DataBHXHDt, uid, ref signer, ref errMessage, "/Hoso/CKy_Dvi");

                if (dataSign == null)
                {
                    //update trang thai ky loi
                    UpdateStatusHoSo(GuidHS, TrangThaiHoso.KyLoi, errMessage);
                    return false;
                }
                //co the thay doi thoi gian countdown dua vao data tra ve tu sever, default la 300
                if (signer == null)
                {
                    //update trang thai ky loi
                    UpdateStatusHoSo(GuidHS, TrangThaiHoso.KyLoi, "Không tạo được signer");
                    return false;
                }
                TSDHashSigner TSDSigner = new TSDHashSigner
                {
                    Id = dataSign.transaction_id,
                    GuidHS = GuidHS,
                    Signer = signer
                };
                listSigner.Add(TSDSigner);
                UpdateStatusHoSo(GuidHS, TrangThaiHoso.DaKyHash, "", dataSign.transaction_id, "", dataSign.transaction_id, dataSign.tran_code);
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
        public void GetResultHoSo_VNPT()
        {
            List<string> ListHSKoLayDuocKQ = new List<string>();
            List<string> ListHetHan = new List<string>();
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
                    string signerId = MethodLibrary.SafeString(dr["SignerId"]);
                    DateTime LastGet = MethodLibrary.SafeDateTime(dr["LastGet"]);
                    string tran_id = MethodLibrary.SafeString(dr["transaction_id"]);
                    //string tenToKhai = MethodLibrary.SafeString(dr["TenToKhai"]);
                    string url = $"{VNPT_URI.uriGetResult}/{tran_id}/status";
                    int typeDK = MethodLibrary.SafeNumber<int>(dr["typeDK"]);
                    string maNV = MethodLibrary.SafeString(dr["MaNV"]);

                    try
                    {

                        ResStatus res = _smartCAService.GetStatus(url);
                       
                        if (res == null)
                        {
                            if (!ListHSKoLayDuocKQ.Contains(GuidHS))
                            {
                                ListHSKoLayDuocKQ.Add(GuidHS);
                            }
                            continue;
                        }

                        if(res.message == "PENDING")
                        {
                            // neu chua lay ket qua do chua ky ben app vnpt cho chay lay tiep cac ket qua khac
                            continue;
                        }

                        if (res.message == "EXPIRED")
                        {
                            //khi to khai da het han update trang thai
                            UpdateStatusHoSo(GuidHS, TrangThaiHoso.HetHan, "File [BHXHDienTu.xml] is expired");
                            if (!ListHetHan.Contains(GuidHS))
                            {
                                ListHetHan.Add(GuidHS);
                            }
                            continue;
                        }
                        if (res.message == "REJECTED")
                        {
                            //khi to khai da het han update trang thai
                            UpdateStatusHoSo(GuidHS, TrangThaiHoso.HetHan, "File [BHXHDienTu.xml] is rejected");
                            if (!ListHSKoLayDuocKQ.Contains(GuidHS))
                            {
                                ListHSKoLayDuocKQ.Add(GuidHS);
                            }
                            continue;
                        }
                        bool isSigned = false;
                        string pathSaved = "";
                        if(typeDK == 0)
                        {
                             pathSaved = Path.Combine(SignedTempFolder, GuidHS, "BHXHDienTu.xml");
                        }
                        else if(typeDK == 1 || typeDK == 2)
                        {
                             pathSaved = Path.Combine(SignedTempFolder, GuidHS, $"{maNV}.xml");
                        }
                        
                        TSDHashSigner TSDSigner = listSigner.FirstOrDefault(s => s.Id == signerId);
                        if (TSDSigner == null)
                        {
                            UpdateStatusHoSo(GuidHS, TrangThaiHoso.KyLoi, "Không tìm được signer");
                            continue;
                        }
                        isSigned = GetResult_Xml(TSDSigner.Signer, res.data, pathSaved);
                        if (!isSigned)
                        {
                            if (!ListHSKoLayDuocKQ.Contains(GuidHS))
                            {
                                ListHSKoLayDuocKQ.Add(GuidHS);
                            }
                            continue;
                        }
                        //update thanh trang thai da ky
                        UpdateStatusHoSo(GuidHS, TrangThaiHoso.DaKy, "", "", pathSaved);
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
                UpdateHoSo_Expired(ListHetHan);
                UpdateHoSo_SignFailed(ListHSKoLayDuocKQ);
            }
            catch (DatabaseInteractException ex)
            {
                Utilities.logger.ErrorLog(ex, "GetResultToKhai_VNPT");
            }
        }
        #endregion

        #region Hoso dang ky
        //Ham SignHash
        public void SignHSDK_Type1()
        {
            try
            {
                //voi cac ho so dang ky type 1 thi chi gom file xml ko can tao file tokhai ma ky thang vao file ho so 
                //vi ko tao file xml hoso nen trangthai=4 (chua tao file) chi de select cac ho so moi, chu ko co y nghia nhu voi hsnghiepvu(typeDk=0) hay hosocapmalandau(typeDk=2)
                string TSQL = $"SELECT TOP {_signHSDKCount} * FROM HoSo_VNPT WITH (NOLOCK) WHERE TrangThai=4 AND typeDK=1 AND LastGet < DATEADD(SECOND, -10, GETDATE()) ORDER BY NgayGui";
                DataTable dt = _dbService.GetDataTable(TSQL);
                if(dt == null || dt.Rows.Count == 0)
                {
                    return;
                }
                foreach( DataRow row in dt.Rows)
                {
                    try
                    {
                        string GuidHS = row["Guid"].SafeString();
                        string pathHSFolder = Path.Combine(SignedTempFolder, GuidHS);
                        string uid = MethodLibrary.SafeString(row["uid"]);
                        string serialNumber = MethodLibrary.SafeString(row["SerialNumber"]);
                        string maNV = MethodLibrary.SafeString(row["MaNV"]);
                        //string tenFile = $"{maNV}.xml";

                        bool isSigned = SignHoSoBHXH(uid, GuidHS, serialNumber, 1, maNV);
                        if (!isSigned)
                        {
                            Utilities.logger.InfoLog($"Hồ sơ ký lỗi không lấy kết quả: {GuidHS}", "Hồ sơ ký lỗi");
                            //Xoa thu muc neu ky loi
                            Directory.Delete(pathHSFolder, true);
                        }
                    }
                    catch (Exception ex)
                    {
                        Utilities.logger.ErrorLog(ex, "SignHSDK_Type1");
                        continue;
                    }
                }
            }
            catch 
            {
                return;
            }
        }

        internal void UpdateHoSoKyLoi()
        {
            List<string> listHSLoi = new List<string>();    
            try
            {
                string TSQL = "SELECT * FROM ToKhai_VNPT WHERE TrangThai=0";
                DataTable dt = _dbService.GetDataTable(TSQL);
                if (dt == null || dt.Rows.Count == 0)
                {
                    return;
                }
                foreach (DataRow row in dt.Rows)
                {
                    if (!listHSLoi.Contains(row["GuidHS"].SafeString()))
                    {
                        listHSLoi.Add(row["GuidHS"].SafeString());  
                    }
                }
            }
            catch (Exception)
            {

                throw;
            }
        }

        #endregion
        //[Obsolete]
        //private SignerProfile RestoreSignerXML(string signerPath, bool isTokhai = true)
        //{
        //    try
        //    {
        //        SignerProfile signerProfile = MethodLibrary.ImportSigner(signerPath);
        //        if (signerProfile == null)
        //        {
        //            return null;
        //        }
        //        //IHashSigner signer = HashSignerFactory.GenerateSigner(signerInfo.UnsignData, signerInfo.SignerCert, null, HashSignerFactory.XML);
        //        IHashSigner signer = MethodLibrary.GenerateCustomSigner(signerInfo.UnsignData, signerInfo.SignerCert);
        //        signer.SetHashAlgorithm(MessageDigestAlgorithm.SHA256);

        //        ((CustomXmlSigner)signer).SetSignatureID(signerInfo.SigId);
        //        ((CustomXmlSigner)signer).SetSigningTime(signerInfo.SigningTime, "SigningTime-" + signerInfo.SigningTimeId);
        //        if (isTokhai)
        //        {
        //            ((CustomXmlSigner)signer).SetParentNodePath("//Cky");
        //        }
        //        else
        //        {
        //            ((CustomXmlSigner)signer).SetParentNodePath("/Hoso/CKy_Dvi");
        //        }
        //        signer.GetSecondHashAsBase64();
        //        return signer;
        //    }
        //    catch (Exception ex)
        //    {
        //        Utilities.logger.ErrorLog(ex, "");
        //        return null;
        //    }
        //}

        //[Obsolete]
        //private DataSign SignSmartCAXML(UserCertificate userCert, string FileBHXHPath, string uid, out SignerProfile signerProfile, string nodeKy = "")
        //{
        //    IHashSigner signer = null;
        //    signerProfile = new SignerProfile();
        //    try
        //    {
        //        byte[] xmlUnsign = File.ReadAllBytes(FileBHXHPath);
        //        String certBase64 = userCert.cert_data;
        //        signer = MethodLibrary.GenerateCustomSigner(xmlUnsign, certBase64);
        //        signer.SetHashAlgorithm(MessageDigestAlgorithm.SHA256);

        //        ((CustomXmlSigner)signer).SetSignatureID("sigid");
        //        //Set reference đến id
        //        //((XmlHashSigner)signers).SetReferenceId("#SigningData");

        //        //Set thời gian ký
        //        ((CustomXmlSigner)signer).SetSigningTime(DateTime.Now, "proid");

        //        //đường dẫn dẫn đến thẻ chứa chữ ký 
        //        if (nodeKy == "")
        //        {
        //            nodeKy = "//Cky";
        //        }
        //        ((CustomXmlSigner)signer).SetParentNodePath(nodeKy);

        //        signerProfile = signer.GetSignerProfile();
        //        var hashValue = Convert.ToBase64String(signerProfile.SecondHashBytes);

        //        var data_to_be_sign = BitConverter.ToString(Convert.FromBase64String(hashValue)).Replace("-", "").ToLower();

        //        DataSign dataSign = _smartCAService.Sign(VNPT_URI.uriSign, data_to_be_sign, userCert.serial_number, uid, "xml");

        //        return dataSign;

        //    }
        //    catch (Exception ex)
        //    {
        //        Utilities.logger.ErrorLog(ex, "SignSmartCAXML", userCert.cert_subject);
        //        signerProfile = null;
        //        return null;
        //    }
        //}
    }
}
