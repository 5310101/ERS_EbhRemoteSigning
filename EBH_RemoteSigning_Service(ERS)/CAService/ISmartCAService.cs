using EBH_RemoteSigning_Service_ERS.Request;
using EBH_RemoteSigning_Service_ERS.Response;
using System;
using System.Collections.Generic;

namespace EBH_RemoteSigning_Service_ERS.CAService
{
    public interface ISmartCAService
    {
        List<UserCertificate> GetListAccountCert(String uri);
        UserCertificate GetAccountCert(String uri, string serialNumber);
        ResSign Sign(String uri, List<SignFile> sign_files, String serialNumber);
        DataTransaction GetStatus(String uri);

    }
}
