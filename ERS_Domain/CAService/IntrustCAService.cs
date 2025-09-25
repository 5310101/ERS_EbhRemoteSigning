using ERS_Domain.Cache;
using ERS_Domain.clsUtilities;
using ERS_Domain.Response;
using IntrustCA_Domain;
using IntrustCA_Domain.Dtos;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERS_Domain.CAService
{
    public class IntrustCAService 
    {
        private readonly IntrustRemoteSigningService _signingService;
        private readonly DbService _dbService;

        public IntrustCAService(string uid, string serial = "")
        {
            _signingService = CreateOrGetService(uid);
            _dbService = new DbService();
        }

        /// <summary>
        /// check cache xem có phien ky chua, neu chua thi tao moi
        /// </summary>
        /// <returns></returns>
        private IntrustRemoteSigningService CreateOrGetService(string uid, string serial = "")
        {
            ICACertificate cert = GetAccountCert(uid, serial);
            if (cert == null) throw new Exception("Không tìm thấy chữ ký số");
            SignSessionStore store = SessionCache.GetOrSetStore(uid,cert);
            return new IntrustRemoteSigningService(store);
        }


        public ICACertificate[] GetAccountCerts(string uid)
        {
            return IntrustRemoteSigningService.GetCertificate(uid);
        }

        public ICACertificate GetAccountCert(string uid, string serialNumber = "")
        {
            var certs = IntrustRemoteSigningService.GetCertificate(uid,serialNumber);
            if (!certs.Any()) return null; 
            return certs[0];
        }
    }
}