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

namespace EBH_RemoteSigning_ver2
{
    public class CoreService
    {
        private IRemoteSignService _signService;

        public CoreService(IRemoteSignService signService)
        {
            _signService = signService;
        }

        public bool SignToKhai_VNPT(ToKhaiInfo tokhai, string uid, string serialNumber)
        {
            try
            {
                DataSign dataSign = null;
                IHashSigner signer = null;
                //var cert = _s ignService.GetAccountCert(VNPT_URI.uriGetCert, uid, serialNumber);
                UserCertificate cert = _signService.GetAccountCert(VNPT_URI.uriGetCert_test, uid, serialNumber);
                switch (tokhai.type)
                {
                    case FileType.PDF:
                        dataSign = SignSmartCAPDF(cert, tokhai.Data);
                        break;
                    case FileType.XML:
                        dataSign = SignSmartCAXML(cert, tokhai.Data, out signer);
                        break;
                    case FileType.OFFICE:
                        
                        break;
                    default:
                        return false;
                }
                if (dataSign == null)
                {
                    return false;
                }
                if(signer != null)
                {
                    bool isExported =  ExportSigner();
                    if (!isExported)
                    {
                        return false;
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                Utilities.logger.ErrorLog(ex, "SignToKhai_VNPT");
                return false;
            }
        }

        private bool ExportSigner()
        {
            //xu ly export signer o day
            return true;
        } 

        private DataSign SignSmartCAPDF(UserCertificate userCert, byte[] pdfUnsign)
        {
            try
            {
                if(pdfUnsign == null) { return null; }  
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
                Utilities.logger.ErrorLog(ex, "SignSmartCAPDF");
                return null;
            }
        }

        private DataSign SignSmartCAXML(UserCertificate userCert, byte[] xmlUnsign, out IHashSigner signer, string nodeKy = "")
        {
            try
            {
                String certBase64 = userCert.cert_data;
                signer = HashSignerFactory.GenerateSigner(xmlUnsign, certBase64, null, HashSignerFactory.XML);
                signer.SetHashAlgorithm(MessageDigestAlgorithm.SHA256);

                //Set ID cho thẻ ssignature
                //((XmlHashSigner)signer).SetSignatureID(Guid.NewGuid().ToString());
                ((XmlHashSigner)signer).SetSignatureID(Guid.NewGuid().ToString());

                //Set reference đến id
                //((XmlHashSigner)signers).SetReferenceId("#SigningData");

                //Set thời gian ký
                ((XmlHashSigner)signer).SetSigningTime(DateTime.Now, "SigningTime-" + Guid.NewGuid().ToString());

                //đường dẫn dẫn đến thẻ chứa chữ ký 
                if (nodeKy != "")
                {
                    ((XmlHashSigner)signer).SetParentNodePath(nodeKy);
                }
                else
                {
                    ((XmlHashSigner)signer).SetParentNodePath("//Cky");
                }


                var hashValue = signer.GetSecondHashAsBase64();

                var data_to_be_sign = BitConverter.ToString(Convert.FromBase64String(hashValue)).Replace("-", "").ToLower();

                DataSign dataSign = _signService.Sign(VNPT_URI.uriSign_test, data_to_be_sign, userCert.serial_number);

                //Console.WriteLine(string.Format("Wait for user confirm: Transaction_id = {0}", dataSign.transaction_id));
                //Console.ReadKey();
                return dataSign;

            }
            catch (Exception ex)
            {
                //log ex
                signer = null;
                return null;
            }
        }

        private DataSign SignSmartCAOFFICE(UserCertificate userCert, string officeInput, out IHashSigner signer)
        {
            String certBase64 = userCert.cert_data;
            signer = null;
            byte[] unsignData = null;
            try
            {
                unsignData = File.ReadAllBytes(officeInput);
            }
            catch (Exception ex)
            {
                //_log.Error(ex);
                return null;
            }
            signer = HashSignerFactory.GenerateSigner(unsignData, certBase64, null, HashSignerFactory.OFFICE);
            signer.SetHashAlgorithm(MessageDigestAlgorithm.SHA256);


            var hashValue = signer.GetSecondHashAsBase64();

            var data_to_be_sign = BitConverter.ToString(Convert.FromBase64String(hashValue)).Replace("-", "").ToLower();

            DataSign dataSign = _signService.Sign(VNPT_URI.uriSign_test, data_to_be_sign, userCert.serial_number);

            return dataSign;
        }

        public async Task GetResultToKhai()
        {

        }
    }
}