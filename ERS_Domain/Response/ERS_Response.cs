using ERS_Domain.Response;

namespace ERS_Domain
{


    public class ERS_Response
    {
        public ERS_Response()
        {
        }

        public ERS_Response(string message) 
        {
            this.message = message; 
        }

        public ERS_Response(string message, bool success) : this(message)
        {
            this.success = success; 
        }

        public ERS_Response(string message, bool success, string data): this(message, success) 
        {
            this.data = data;
        }

        public ERS_Response(string message, bool success, UserCertificate[] certificates) : this(message, success)
        {
            this.certificates = certificates;
        }

        public string message { get; set; } 
        public bool success { get; set; }
        public UserCertificate[] certificates { get; set; }
        public string data {  get; set; }   
    }
}