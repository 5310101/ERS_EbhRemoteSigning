using IntrustCA_Domain.Dtos;
using System;
using System.Linq;
using System.IO;
using IntrustderCA_Domain;
using System.Collections.Generic;

namespace IntrustCA_Domain
{
    /// <summary>
    /// Chi dung hinh thuc ky qua app, ko dung ma pin
    /// </summary>
    public class IntrustRemoteSigningService
    {
        private readonly SignSessionStore _signSessionStore;

        public IntrustRemoteSigningService(SignSessionStore store)
        {
            _signSessionStore = store;
        }

        /// <summary>
        /// ky tu xa
        /// </summary>
        /// <param name="fileDic">dictionary voi key la inpath file va value la output file</param>
        /// <returns></returns>
        public FileSigned[] SignRemote(string input, string output, FileToSignDto<FileProperties>[] lstFile)
        {
            if (_signSessionStore.IsSessionValid == false)
            {
                throw new Exception("Cannot create valid sign session");
            }
            var req = new SignRequest
            {
                user_name = _signSessionStore.UserName,
                credentialID = _signSessionStore.Cert.key_id,
                certificate = _signSessionStore.Cert.cert_content,
                serial_number = _signSessionStore.Cert.cert_serial,
                signed_with_session = true,
                is_use_request_login = true,
                auth_data = _signSessionStore.AuthData,
                files = lstFile
            };
            var res = IntrustSigningCoreService.SignRemote(req);
            if (res.status != "success")
            {
                throw new Exception("Sign remote failed: " + res.error_desc);
            }
            if (res.files.Any() == false)
            {
                throw new Exception("File cannot sign");
            }
            //tra ve result tung file de xu ly o service ngoai
            return res.files;
        }

        //Ham test ky tung file
        public bool SignRemoteOneFile(string input, string output, FileProperties properties = null)
        {

            if (_signSessionStore.IsSessionValid == false)
            {
                return false;
            }
            var file = new FileToSignDto<FileProperties>
            {
                file_id = Guid.NewGuid().ToString(),
                file_name = System.IO.Path.GetFileName(input),
                content_file = Convert.ToBase64String(System.IO.File.ReadAllBytes(input)),
                extension = System.IO.Path.GetExtension(input).TrimStart('.').ToLower(),
            };
            if (properties != null)
            {
                file.properties = properties;
            }
            else
            {
                file.properties = CreatePropertiesDefault(file.extension);
            }
            var req = new SignRequest
            {
                user_name = _signSessionStore.UserName,
                credentialID = _signSessionStore.Cert.key_id,
                certificate = _signSessionStore.Cert.cert_content,
                serial_number = _signSessionStore.Cert.cert_serial,
                signed_with_session = true,
                is_use_request_login = true,
                auth_data = _signSessionStore.AuthData,
                files = new FileToSignDto<FileProperties>[] { file }
            };
            var res = IntrustSigningCoreService.SignRemote(req);
            if (res.status != "success")
            {
                throw new Exception("Sign remote failed: " + res.error_desc);
            }
            if (res.files.Any() == false)
            {
                throw new Exception("File cannot sign");
            }
            FileSigned fileSigned = res.files.First();
            if (fileSigned.status != "success")
            {
                throw new Exception($"Error happened when signing file: {fileSigned.error_message}");
            }
            string base64Str = fileSigned.content_file;
            byte[] dataBytes = base64Str.Base64ToData();
            File.WriteAllBytes(output, dataBytes);
            return true;
        }

        private FileProperties CreatePropertiesDefault(string tenFile)
        {
            string extension = Path.GetExtension(tenFile);
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
                        return new XmlProperties
                        {
                            option_xml_form = "B_H",
                            date_sign = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                            tag_signature = "TK1-TS/Cky",
                            tag_id = ""
                        };
                    }
                default:
                    throw new Exception("Only support PDF and XML file type");
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
