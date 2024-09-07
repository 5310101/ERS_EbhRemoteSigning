using EBH_RemoteSigning_Service_ERS.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace EBH_RemoteSigning_Service_ERS
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

        public ERS_Response(string message, bool success, DataTransaction transaction): this(message, success) 
        {
            this.transaction = transaction;
        }

        public ERS_Response(string message, bool success, UserCertificate[] certs) : this(message, success)
        {
            this.certs = certs;
        }
        public ERS_Response(string message, bool success, DataSign dataSign) : this(message, success)
        {
            this.dataSign = dataSign;
        }

        public string message { get; set; } 
        public bool success { get; set; }
        public DataTransaction transaction { get; set; }
        public UserCertificate[] certs { get; set; } 
        public DataSign dataSign { get; set; }
    }
}