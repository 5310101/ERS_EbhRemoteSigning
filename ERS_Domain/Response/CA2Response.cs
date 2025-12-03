namespace ERS_Domain.Response
{
    public class CA2Response<TData>
    {
        public TData data { get; set; }
        public string message { get; set; }
        public int status_code { get; set; }
    }

    public class CA2Certificates
    {
        public string transaction_id { get; set; }
        public CA2Certificate[] user_certificates { get; set; }
    }

    public class CA2Certificate
    {
        public string cert_data { get; set; }
        public string cert_id { get; set; }
        public CA2Chain chain_data { get; set; }
        public string serial_number { get; set; }
    }

    public class CA2Chain
    {
        public string ca_cert { get; set; }
        public string root_cert { get; set; }
    }

    //response sign
    public class FileSigned
    {
        public string transaction_id { get; set; }
        public string[] sign_files { get; set; }
    }

    //response status check
    public class CA2StatusCheckResponse
    {
        public string sp_id { get; set; }
        public int status_code { get; set; }
        public string message  { get; set; }
        public CA2SignedResult data { get; set; }
    }

    public class CA2SignedResult
    {
        public string transaction_id { get; set; }
        public CA2SignatureData[] signatures { get; set; }
    }

    public class CA2SignatureData
    {
        public string doc_id { get; set; }
        public string signature_value { get; set; }
        public string timestamp_signature { get; set; }
    }
}
