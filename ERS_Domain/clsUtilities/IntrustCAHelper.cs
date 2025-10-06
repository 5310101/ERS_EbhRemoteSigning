using ERS_Domain.Response;
using IntrustCA_Domain;
using IntrustCA_Domain.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.RightsManagement;
using System.Text;
using System.Threading.Tasks;

namespace ERS_Domain.clsUtilities
{
    public class IntrustCAHelper
    {
        public static UserCertificate[] GetIntrustCertificates(string uid, string serialNumber = "")
        {
            GetCertificateRequest req = new GetCertificateRequest
            {
                user_id = uid,
                serial_number = serialNumber
            };
            var res = IntrustSigningCoreService.GetCertificate(req);
            if (res == null || res.status_code != 0 || res.certificates == null || res.certificates.Length == 0)
            {
                throw new Exception($"Get certificate error: {res?.error_desc ?? "No response from IntrustCA"}");
            }
            List<UserCertificate> lstCert = new List<UserCertificate>();
            foreach (ICACertificate IntrustCert in res.certificates)
            {
                UserCertificate cert = new UserCertificate
                {
                    serial_number = IntrustCert.cert_serial,
                    cert_valid_to = IntrustCert.cert_valid_to.SafeDateTime(),
                    cert_valid_from = IntrustCert.cert_valid_from.SafeDateTime(),
                    cert_subject = IntrustCert.cert_provider,
                    cert_status = IntrustCert.cert_valid_to.SafeDateTime() < DateTime.Now ? "Hết hạn" : "Đang hoạt động",
                };
                lstCert.Add(cert);
            }
            return lstCert.ToArray();
        }
    }
}
