using CA2_Winservice;
using CA2_Winservice.Consumer;
using CA2_Winservice.Services;
using ERS_Domain;
using ERS_Domain.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IntrustCA_Winservice.Process
{
    /// <summary>
    /// process nay se chua nhieu consumer de xu ly tung loai messsage loi
    /// </summary>
    public class HandleErrorProcess
    {
        private readonly RabbitmqManager _manager;
        private readonly CoreService _coreService;
        private readonly ushort numberDeadLetterPerProcess ;
        private readonly List<DLQConsumer> _dLQConsumers = new List<DLQConsumer>();
        private readonly string[] queueNames = { "HSCA2.dlq", "HSCA2.ToKhai.dlq", "HSCA2.ReadyToSign.dlq" };

        public HandleErrorProcess(RabbitmqManager manager, CoreService coreService)
        {
            _manager = manager;
            _coreService = coreService;
            if (!ushort.TryParse(System.Configuration.ConfigurationManager.AppSettings["NUMBERDL_PERPROCESS"], out numberDeadLetterPerProcess))
            {
                numberDeadLetterPerProcess = 10;
            }
            _dLQConsumers.AddRange(CreateConsumer());
        }

        private IEnumerable<DLQConsumer> CreateConsumer()
        {
            return queueNames.Select(name => new DLQConsumer(_manager.CreateChanel(), HandleDLQMessage, name, numberDeadLetterPerProcess));
        }

        //Tam thoi chua biet lam gi voi cac message nay nen chi ghi ra file roi ack luon
        //trong tuong lai co the xay dung cac he thong monitor cung nhu co co che resend
        //Neu co thay doi trong cach xu ly thi thay doi ham HandleDLQMessage
        public void DoWork()
        {
            foreach (var consumer in _dLQConsumers)
            {
                consumer.ConsumeMessage();
            }
        }

        //Tam thoi dung chung 1 ham cho tat ca dlq, sau can xu ly theo kieu gi thi thay the
        public bool HandleDLQMessage(HoSoMessage hs, string queueName)
        {
            try
            {
                Utilities.logger.InfoLog($"Handle dead letter message from queue {queueName} with uid {hs?.uid}", "HandleErrorProcess");
                //Them logic xu ly tai day

                return true;
            }
            catch (Exception ex)
            {
                Utilities.logger.ErrorLog(ex, "HandleDLQMessage");
                return false;
            }
        }
    }
}
