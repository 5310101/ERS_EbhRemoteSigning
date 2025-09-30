using IntrustCA_Domain.Dtos;
using IntrustCA_Domain;
using System;
using System.Linq;
using System.IO;

namespace IntrustderCA_Domain.Dtos
{
    public static class IntrustRSHelper
    {
        public static FileProperties CreatePropertiesDefault(string tenFile)
        {
            string extension = Path.GetExtension(tenFile).TrimStart('.').ToLower();
            switch (extension)
            {
                case "pdf":
                    return new PdfProperties
                    {
                        pageNo = "1",
                        coorDinate = "0,0,200,100",
                        signTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                        positionIdentifier = "",
                        rectangleOffset = "50,50",
                        rectangleSize = "200,100",
                        showSignerInfo = true,
                        showDatetime = true,
                        showSignIcon = true,
                        showReason = true,
                    };
                case "xml":
                    {
                        string nodeKy = GetNodeSign_Xml(Path.GetFileNameWithoutExtension(tenFile));
                        return new XmlProperties
                        {
                            option_xml_form = "B_H",
                            date_sign = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                            tag_signature = nodeKy,
                            tag_id = ""
                        };
                    }
                default:
                    throw new Exception("Only support PDF and XML file type");
            }
        }

        public static string GetNodeSign_Xml(string fileName)
        {
            switch (fileName)
            {
                case "TK1-TS-595":
                    return "TK1-TS/Cky";
                case "D02-TS-595":
                    return "D02-TS/Cky";
                case "D03-TS-595":
                    return "D03-TS/Cky";
                case "D05-TS-595":
                    return "D05-TS/Cky";
                case "M01B-HSB":
                    return "M01B-HSB/Cky";
                case "05A-HSB":
                    return "M05A-HSB/Cky";
                case "D01-TS-595":
                    return "D01-TS/Cky";
                case "BHXHDienTu":
                    return "Hoso/CKy_Dvi";
                default: throw new Exception($"Unknown file name {fileName}");
            }
        }

        public static ICACertificate[] GetCertificates(string userName, string serial = "")
        {
            try
            {
                GetCertificateRequest req = new GetCertificateRequest
                {
                    user_id = userName,
                    serial_number = serial
                };

                var res = IntrustSigningCoreService.GetCertificate(req);
                if (res.status != "success")
                {
                    throw new Exception("Cannot connect to IntrustCA server");
                }
                if (!res.certificates.Any())
                {
                    throw new Exception("Certificates not found");
                }
                return res.certificates;
            }
            catch (Exception ex)
            {
                Logger.ErrorLog(ex, "GetCertificate", userName, serial);
                return Array.Empty<ICACertificate>();
            }
        }

        public static ICACertificate GetCertificate(string userName, string serial = "")
        {
            try
            {
                GetCertificateRequest req = new GetCertificateRequest
                {
                    user_id = userName,
                    serial_number = serial
                };

                var res = IntrustSigningCoreService.GetCertificate(req);
                if (res.status != "success")
                {
                    throw new Exception("Cannot connect to IntrustCA server");
                }
                if (!res.certificates.Any())
                {
                    throw new Exception("Certificates not found");
                }
                return res.certificates.First();
            }
            catch (Exception ex)
            {
                Logger.ErrorLog(ex, "GetCertificate", userName, serial);
                return null;
            }
        }
    }
}
