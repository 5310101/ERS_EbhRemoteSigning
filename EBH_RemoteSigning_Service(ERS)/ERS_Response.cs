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

        public ERS_Response(string message, bool success, object data): this(message, success) 
        {
            this.Data = data;
        }

        public string message { get; set; } 
        public bool success { get; set; } 
        public object Data {  get; set; }   
    }
}