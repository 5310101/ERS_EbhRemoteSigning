using EBH_RemoteSigning_Service_ERS.Request;
using EBH_RemoteSigning_Service_ERS.Response;
using System;
using System.Collections.Generic;

namespace EBH_RemoteSigning_Service_ERS.CAService
{
    public interface ISmartCAService
    {
        UserCertificate[] GetListAccountCert(String uri, string uid);
        UserCertificate GetAccountCert(String uri, string uid, string serialNumber = "");
        ResSign Sign(String uri, List<SignFile> sign_files, string uid, String serialNumber);
        ResStatus GetStatus(String uri);

    }
}
