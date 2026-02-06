using ERS_Domain.MISA.Dto;
using System.Threading.Tasks;
using System;

namespace ERS_Domain.MISA.SignService
{
    public class MISASignService
    {
        private readonly MISAAPIConfig _config;
        private readonly HttpSendRequest _send;

        public MISASignService(MISAAPIConfig config)
        {
            _config = config;
            _send = new HttpSendRequest();
        }

        public async Task<MISASigninResponse> SignIn(MISASigninRequest req)
        {
            var res = await _send.SendRequestAsync<MISASigninResponse>(HttpMethodType.post, MISAAPIUrl.signIn, req);
            if (res == null) throw new Exception("Cannot connect to server");
            return res;
        }

    }
}
