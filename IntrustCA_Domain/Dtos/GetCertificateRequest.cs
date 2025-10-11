namespace IntrustCA_Domain.Dtos
{
    public class GetCertificateRequest
    {
        public string user_id { get; set; }
        public string serial_number { get; set; }
    }
}
