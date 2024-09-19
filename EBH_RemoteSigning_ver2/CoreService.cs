using ERS_Domain;
using ERS_Domain.CAService;
using ERS_Domain.clsUtilities;
using ERS_Domain.Model;
using ERS_Domain.Response;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using VnptHashSignatures.Common;
using VnptHashSignatures.Interface;
using VnptHashSignatures.Pdf;
using VnptHashSignatures.Xml;
using System.Data.SqlClient;
using Microsoft.Win32.SafeHandles;
using System.Security.Cryptography;

namespace EBH_RemoteSigning_ver2
{
    public class CoreService
    {
        private IRemoteSignService _signService;
        private DbService _dbService;

        public CoreService(IRemoteSignService signService, DbService dbService)
        {
            _signService = signService;
            _dbService = dbService;
        }

        public bool SignToKhai_VNPT(List<ToKhaiInfo> tokhais, string GuidHS, string uid, string serialNumber)
        {
            try
            {
                //var cert = _signService.GetAccountCert(VNPT_URI.uriGetCert, uid, serialNumber);
                UserCertificate cert = _signService.GetAccountCert(VNPT_URI.uriGetCert_test, serialNumber);
                List<SignedHashInfo> listSignedDTO = new List<SignedHashInfo>();
                foreach (ToKhaiInfo tokhai in tokhais)
                {
                    DataSign dataSign = null;
                    SignerInfo signerInfo = null;
                    SignedHashInfo signedHashInfo = new SignedHashInfo();
                    signedHashInfo.ToKhai = tokhai;
                    switch (tokhai.Type)
                    {
                        case FileType.PDF:
                            dataSign = SignSmartCAPDF(cert, tokhai.Data);
                            break;
                        case FileType.XML:
                            dataSign = SignSmartCAXML(cert, tokhai.Data, out signerInfo);
                            break;
                        case FileType.OFFICE:
                            dataSign = SignSmartCAOFFICE(cert, tokhai.Data, out signerInfo);
                            break;
                        default:
                            return false;
                    }
                    //1 file ky loi thi return luon
                    if (dataSign == null)
                    {
                        return false;
                    }
                    signedHashInfo.SignData = dataSign;
                    if (signerInfo == null)
                    {
                        continue;
                    }
                    signedHashInfo.Signer = signerInfo;
                    listSignedDTO.Add(signedHashInfo);
                }

                //Khi da co datasign thi se tao thu muc chua ho so ky
                string pathTempHS = Path.Combine(Utilities.globalPath.SignedTempFolder, $"{GuidHS}");
                if (!Directory.Exists(pathTempHS))
                {
                    Directory.CreateDirectory(pathTempHS);
                }

                string pathSigner = "";
                foreach (var signedHash in listSignedDTO)
                {
                    if (signedHash.Signer != null)
                    {
                        pathSigner = ExportSigner(signedHash.Signer, pathTempHS, signedHash.SignData.transaction_id);
                        if (pathSigner == "")
                        {
                            return false;
                        }
                        signedHash.PathSigner = pathSigner;
                    }
                }

                //luu cac to khai vao database
                //bool isInserted =  InsertDatabase_ToKhai(tokhai, GuidHS, dataSign, pathSigner);
                bool isInserted = InsertDatabase_ToKhai(listSignedDTO, GuidHS);
                //neu insert loi delete thu muc temp cua ho so
                if (!isInserted)
                {
                    Directory.Delete(pathTempHS, true);
                } 
                return isInserted;
            }
            catch (Exception ex)
            {
                Utilities.logger.ErrorLog(ex, "SignToKhai_VNPT");
                return false;
            }
        }

        private bool InsertDatabase_ToKhai(List<SignedHashInfo> signedHashs, string GuidHS)
        {
            List<string> listTranId_Error = new List<string>();
            using (SqlConnection conn = new SqlConnection(_dbService.ConnStr))
            {
                conn.Open();
                SqlTransaction trans  = conn.BeginTransaction();
                try
                {
                    foreach (SignedHashInfo shi in signedHashs)
                    {
                        string TSQL = "INSERT INTO ToKhai (GuidHS,TenToKhai,LoaiFile,MoTa,NgayGui,TrangThai,SignerPath,transaction_id,tran_code) VALUES (@GuidHS,@TenToKhai,@LoaiFile,@Mota,@NgayGui,@TrangThai,@SignerPath,@transaction_id,@tran_code)";
                        var listParams = new SqlParameter[]
                        {
                            new SqlParameter("@GuidHS",GuidHS),
                            new SqlParameter("@TenToKhai",shi.ToKhai.TenFile),
                            new SqlParameter("@LoaiFile", (int)shi.ToKhai.Type),
                            new SqlParameter("@MoTa",shi.ToKhai.TenToKhai),
                            new SqlParameter("@NgayGui",DateTime.Now),
                            new SqlParameter("@TrangThai", (int)TrangThaiFile.DaKyHash),
                            new SqlParameter("@SignerPath", shi.PathSigner),
                            new SqlParameter("@transaction_id",shi.SignData.transaction_id),
                            new SqlParameter("@tran_code", shi.SignData.tran_code)
                        };
                        using (SqlCommand command = new SqlCommand(TSQL, conn, trans))
                        {
                            command.Parameters.AddRange(listParams);
                            command.CommandType = System.Data.CommandType.Text;
                            command.CommandTimeout = 60;
                            var result = command.ExecuteNonQuery();
                            if(result <= 0)
                            {
                                listTranId_Error.Add(shi.SignData.transaction_id);
                                throw new Exception("Insert Failed");
                            }
                        }
                    }
                    trans.Commit();
                    return true;
                }
                catch (Exception ex)
                {
                    trans.Rollback();   
                    Utilities.logger.ErrorLog(ex, "InsertDatabase_ToKhai",GuidHS,string.Join(", ",listTranId_Error));
                    return false;
                }
            }

        }

        //luu tru thong tin signer de sau khi lay ket qua tao signer moi
        private string ExportSigner(SignerInfo signer, string pathTempHS, string transaction_id)
        {
            try
            {
                string json = JsonConvert.SerializeObject(signer);
                if (string.IsNullOrEmpty(json))
                {
                    return "";
                }
                File.WriteAllText(pathTempHS, json);
                return pathTempHS;
            }
            catch (Exception ex)
            {
                Utilities.logger.ErrorLog(ex, "ExportSigner");
                return "";
            }
        }
        //import lai thong tin signer da luu tru
        private SignerInfo ImportSigner(string filePath)
        {
            string json = File.ReadAllText(filePath);
            SignerInfo output = JsonConvert.DeserializeObject<SignerInfo>(json);
            return output;
        }

        #region cac ham ky remote
        private DataSign SignSmartCAPDF(UserCertificate userCert, byte[] pdfUnsign)
        {
            try
            {
                if (pdfUnsign == null) { return null; }
                IHashSigner signer = HashSignerFactory.GenerateSigner(pdfUnsign, userCert.cert_data, null, HashSignerFactory.PDF);
                signer.SetHashAlgorithm(MessageDigestAlgorithm.SHA256);

                #region Optional -----------------------------------
                // Property: Lý do ký số
                ((PdfHashSigner)signer).SetReason("Xác nhận tài liệu");
                // Kiểu hiển thị chữ ký (OPTIONAL/DEFAULT=TEXT_WITH_BACKGROUND) 
                ((PdfHashSigner)signer).SetRenderingMode(PdfHashSigner.RenderMode.TEXT_ONLY);
                // Nội dung text trên chữ ký (OPTIONAL)
                ((PdfHashSigner)signer).SetLayer2Text($"Ngày ký: {DateTime.Now.Date} \n Người ký: QuanNa \n Nơi ký: EBH");
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

                var data_to_be_sign = BitConverter.ToString(Convert.FromBase64String(hashValue)).Replace("-", "").ToLower();

                string tempFolder = Path.GetTempPath();
                File.AppendAllText(tempFolder + data_to_be_sign + ".txt", profileJson);

                DataSign dataSign = _signService.Sign(VNPT_URI.uriSign_test, data_to_be_sign, userCert.serial_number);

                return dataSign;
            }
            catch (Exception ex)
            {
                Utilities.logger.ErrorLog(ex, "SignSmartCAPDF", userCert.cert_subject);
                return null;
            }
        }

        private DataSign SignSmartCAXML(UserCertificate userCert, byte[] xmlUnsign, out SignerInfo signerInfo, string nodeKy = "")
        {
            IHashSigner signer = null;
            signerInfo = new SignerInfo();
            try
            {
                String certBase64 = userCert.cert_data;
                signerInfo.SignerCert = certBase64;
                signerInfo.UnsignData = xmlUnsign;
                signer = HashSignerFactory.GenerateSigner(xmlUnsign, certBase64, null, HashSignerFactory.XML);
                signer.SetHashAlgorithm(MessageDigestAlgorithm.SHA256);


                //Set ID cho thẻ ssignature
                //((XmlHashSigner)signer).SetSignatureID(Guid.NewGuid().ToString());
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

                DataSign dataSign = _signService.Sign(VNPT_URI.uriSign_test, data_to_be_sign, userCert.serial_number);

                //Console.WriteLine(string.Format("Wait for user confirm: Transaction_id = {0}", dataSign.transaction_id));
                //Console.ReadKey();
                return dataSign;

            }
            catch (Exception ex)
            {
                Utilities.logger.ErrorLog(ex, "SignSmartCAXML", userCert.cert_subject);
                signerInfo = null;
                return null;
            }
        }

        private DataSign SignSmartCAOFFICE(UserCertificate userCert, byte[] officeUnsign, out SignerInfo signerInfo)
        {
            try
            {
                IHashSigner signer = null;
                signerInfo = new SignerInfo();

                String certBase64 = userCert.cert_data;
                signerInfo.SignerCert = certBase64;
                signerInfo.UnsignData = officeUnsign;
                signer = HashSignerFactory.GenerateSigner(officeUnsign, certBase64, null, HashSignerFactory.OFFICE);
                signer.SetHashAlgorithm(MessageDigestAlgorithm.SHA256);

                var hashValue = signer.GetSecondHashAsBase64();

                var data_to_be_sign = BitConverter.ToString(Convert.FromBase64String(hashValue)).Replace("-", "").ToLower();

                DataSign dataSign = _signService.Sign(VNPT_URI.uriSign_test, data_to_be_sign, userCert.serial_number);

                return dataSign;
            }
            catch (Exception ex)
            {
                Utilities.logger.ErrorLog(ex, "SignSmartCAOFFICE", userCert.cert_subject);
                throw;
            }
        }
        #endregion

        //neu nguoi dung co tu 2 cks tro len se dung ham nay de lay 
        public UserCertificate[] GetListUserCertificateVNPT()
        {
            UserCertificate[] certs = _signService.GetListAccountCert(VNPT_URI.uriGetCert_test);
            return certs;
        }
    }
}