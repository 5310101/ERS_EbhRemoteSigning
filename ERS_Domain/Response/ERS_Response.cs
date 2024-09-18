using EBH_RemoteSigning_ver2.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

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

        public string message { get; set; } 
        public bool success { get; set; }
        public string data {  get; set; }   
    }
}