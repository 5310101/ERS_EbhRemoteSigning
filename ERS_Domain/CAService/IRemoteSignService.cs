using ERS_Domain.Request;
using ERS_Domain.Response;
using System;
using System.Collections.Generic;

namespace ERS_Domain.CAService
{
    public interface IRemoteSignService
    {
        UserCertificate[] GetListAccountCert(String uri);
        UserCertificate GetAccountCert(String uri, string serialNumber = "");
        DataSign Sign(String uri, string data_to_be_signed, String serialNumber);
        ResStatus GetStatus(String uri);

    }
}
