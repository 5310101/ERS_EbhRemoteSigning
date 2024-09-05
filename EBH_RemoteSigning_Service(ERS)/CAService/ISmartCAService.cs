using EBH_RemoteSigning_Service_ERS.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EBH_RemoteSigning_Service_ERS.CAService
{
    public interface ISmartCAService
    {
        List<UserCertificate> GetListAccountCert(String uri);
        UserCertificate GetAccountCert(String uri, string serialNumber);
        DataSign Sign(String uri, string data_to_be_signed, String serialNumber);
        DataTransaction GetStatus(String uri);

    }
}
