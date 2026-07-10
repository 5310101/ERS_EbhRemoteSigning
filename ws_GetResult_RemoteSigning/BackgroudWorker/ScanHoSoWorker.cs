using ERS_Domain;
using RabbitMQ.Client;
using System;
using System.Configuration;
using System.Threading;
using System.Threading.Tasks;

namespace ws_GetResult_RemoteSigning.BackgroudWorker
{
    public class ScanHoSoWorker
    {
        private readonly ScanHoSoProcess _scanHosoProcess;
        private readonly TimeSpan _timeSpan = TimeSpan.FromSeconds(int.Parse(ConfigurationManager.AppSettings["SCANHS_INTERVAL"]));

        public ScanHoSoWorker(ScanHoSoProcess scanHosoProcess)
        {
            _scanHosoProcess = scanHosoProcess;
        }

        public async Task RunAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await _scanHosoProcess.RunAsync(cancellationToken);
                }
                catch(OperationCanceledException ex)
                {
                    Utilities.logger.InfoLog("Request stop service","service are stopped by request");
                }
                catch (Exception ex)
                {
                    Utilities.logger.ErrorLog(ex,"ScanHosoProcess error");
                }
                await Task.Delay(_timeSpan, cancellationToken);
            }
        }
    }
}
