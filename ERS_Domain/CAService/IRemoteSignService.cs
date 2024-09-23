using ERS_Domain.Request;
using ERS_Domain.Response;
using System;
using System.Collections.Generic;
using System.Data.SqlTypes;

namespace ERS_Domain.CAService
{
    public interface IRemoteSignService
    {
        UserCertificate[] GetListAccountCert(string uri, string uid);
        UserCertificate GetAccountCert(String uri, string uid, string serialNumber = "");
        DataSign Sign(  string uri, string data_to_be_signed, string serialNumber, string uid);
        ResStatus GetStatus(string uri);

    }
}
