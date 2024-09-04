using EBH_RemoteSigning_Service_ERS_.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EBH_RemoteSigning_Service_ERS_.SmartCAService
{
    public interface ISmartCAService
    {
        UserCertificate GetAccountCert(String uri);
        DataSign Sign(String uri, string data_to_be_signed, String serialNumber);
        DataTransaction GetStatus(String uri);

    }
}
