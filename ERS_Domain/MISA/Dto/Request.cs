using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERS_Domain.MISA.Dto
{
    public class MISASigninRequest
    {
        public string userName { get; set; }
        public string password { get; set; }
    }

    public class  MISARefreshTokenRequest
    {
        public string refreshToken { get; set; }
    }

    public class MISATwoFactorRequest
    {
        public string userName { get; set; }
        public string code { get; set; }
        public int otpType { get; set; }
        public bool remember { get; set; }
    }

    public class MISAResendOTPRequest
    {
        public string userName { get; set; }
        public string language { get; set; } = "vi-VN";
    }
