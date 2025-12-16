using ERS_Domain;
using IntrustCA_Winservice.Process;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IntrustCA_Winservice.Worker
{
    public class ScanHoSoWorker
    {
        private readonly ScanHoSoProcess _scanProcess;
        private readonly CancellationToken _cancellationToken;
        private readonly TimeSpan _timeSpan = TimeSpan.FromSeconds(1);

        public ScanHoSoWorker(ScanHoSoProcess scanProcess, CancellationToken cancellationToken)
        {
            _scanProcess = scanProcess;
            _cancellationToken = cancellationToken;
        }

        public async Task RunAsync()
        {
            while (!_cancellationToken.IsCancellationRequested)
            {
                try
                {
                    _scanProcess.StartProcess();
                }
                catch (OperationCanceledException)
                {
                    Utilities.logger.InfoLog("Request stop", "Stop ScanHoSoWorker");
                }
                catch (Exception ex)
                {
                    Utilities.logger.ErrorLog(ex, "Stop ScanHoSoWorker");
                    throw;
                }

                await Task.Delay(_timeSpan, _cancellationToken);
            }
        }
    }
}
