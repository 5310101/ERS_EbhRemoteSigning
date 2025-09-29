using IntrustCA_Domain.Dtos;
using System;
using System.Linq;
using System.IO;
using IntrustderCA_Domain.Dtos;

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
        /// Ky nhieu file trong 1 lan
        /// </summary>
        /// <param name="lstFile">List file can ky</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public FileSigned[] SignRemote( FileToSignDto<FileProperties>[] lstFile)
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
                file.properties = IntrustRSHelper.CreatePropertiesDefault(file.file_name);
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
    }
}
