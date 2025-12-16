using CA2_Winservice.Process;
using ERS_Domain;
using ERS_Domain.clsUtilities;
using ERS_Domain.Exceptions;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CA2_Winservice.BackgroundWorker
{
    public class ScanHoSoWorker
    {
        private readonly ScanHoSoProcess _scanHoSoProcess;
        private readonly CancellationToken _cancellationToken;
        private readonly TimeSpan _timeSpan = TimeSpan.FromSeconds(ConfigurationManager.AppSettings["SCANHS_INTERVAL"].SafeNumber<int>());

        public ScanHoSoWorker(ScanHoSoProcess scanHoSoProcess, CancellationToken cancellationToken)
        {
            _scanHoSoProcess = scanHoSoProcess;
            _cancellationToken = cancellationToken;
        }

        public async Task RunAsync()
        {
            while (!_cancellationToken.IsCancellationRequested)
            {
                try
                {
                    _scanHoSoProcess.StartProcess();
                }
                //Neu bat dc exception nay o ngoai ham chay process thi dung luon worker
                catch(OperationCanceledException ex)
                {
                    Utilities.logger.ErrorLog(ex, "Request Stop");
                }
                catch (Exception ex)
                {
                    Utilities.logger.ErrorLog(ex, "RunAsync");
                    // nem lai de dung worker
                    throw;
                }

                await Task.Delay(_timeSpan, _cancellationToken);
            }
        }
    }
}
