using System;
using System.Collections.Generic;

namespace GatewayServiceTest
{
    /// <summary>
    /// Mapped with service response message
    /// </summary>
    public class ResponseMessage
    {
        public Guid ResponseID { get; set; }
        public int ResponseCode { get; set; }
        public string ResponseContent { get; set; }

        // See reponse to custome this last property
        public object Content { get; set; }
    }

    /// <summary>
    /// Mapped with Content property in response message
    /// </summary>
    public class Account
    {
        public string Email { get; set; }
        public string PhoneNumber { get; set; }

        // -----------------------------------------
        // ...More properties 
        // See json response to add more
        // -----------------------------------------

        // Complex property
        public List<Group> Groups { get; set; }
    }

    /// <summary>
    /// If 'Content' property contain complex property, create new mapped class like this
    /// </summary>
    public class Group
    {
        public string ID { get; set; }
        public string AdminEmail { get; set; }
        public string TenNhom { get; set; }

        // -----------------------------------------
        // ...More properties 
        // See json response to add more
        // -----------------------------------------
    }
}
