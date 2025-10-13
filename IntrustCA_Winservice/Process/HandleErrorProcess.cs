using ERS_Domain;
using ERS_Domain.Dtos;
using IntrustCA_Winservice.Services;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;

namespace IntrustCA_Winservice.Process
{
    /// <summary>
    /// process nay se chua nhieu consumer de xu ly tung loai messsage loi
    /// </summary>
    public class HandleErrorProcess
    {
        private readonly IChannel _channel;
        private readonly CoreService _coreService;
        private readonly ushort numberDeadLetterPerProcess = ushort.Parse(System.Configuration.ConfigurationManager.AppSettings["NUMBERDL_PERPROCESS"]);

        public HandleErrorProcess(IChannel channel, CoreService coreService)
        {
            _channel = channel;
            _coreService = coreService;
        }

        //Tam thoi chua biet lam gi voi cac message nay nen chi ghi ra file roi ack luon
        //trong tuong lai co the xay dung cac he thong monitor cung nhu co co che resend
        public void Dowork()
        {
            
        }
    }
}
