using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace EBH_RemoteSigning_Service_ERS.Response
{
    //Getcert
    public class ResGetCert
    {
        public int status_code { get; set; }
        public string message { get; set; }
        public GetCertData data { get; set; }
    }
    public class GetCertData
    {
        public List<UserCertificate> user_certificates { get; set; }
    }
    public class UserCertificate
    {
        public string service_type { get; set; }
        public string service_name { get; set; }
        public string cert_id { get; set; }
        public string cert_status { get; set; }
        public string serial_number { get; set; }
        public string cert_subject { get; set; }
        public DateTime cert_valid_from { get; set; }
        public DateTime cert_valid_to { get; set; }
        public string cert_data { get; set; }
        public ChainData chain_data { get; set; }
        public string transaction_id { get; set; }
    }

    public class ChainData
    {
        public string ca_cert { get; set; }
        public object root_cert { get; set; }
    }

    //Sign
    public class ResSign
    {
        public int status_code { get; set; }
        public string message { get; set; }
        public DataSign data { get; set; }
    }

    public class DataSign
    {
        public string transaction_id { get; set; }
        public string tran_code { get; set; }
    }

    //Check status
    public class ResStatus
    {
        public int status_code { get; set; }
        public string message { get; set; }
        public DataTransaction data { get; set; }
    }

    public class DataTransaction
    {
        public string transaction_id { get; set; }
        public List<Signature> signatures { get; set; }
    }

    public class Signature
    {
        public string doc_id { get; set; }
        public string signature_value { get; set; }
        public object timestamp_signature { get; set; }
    }
}