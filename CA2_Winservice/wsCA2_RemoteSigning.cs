using CA2_Winservice.BackgroundWorker;
using CA2_Winservice.Process;
using CA2_Winservice.Services;
using ERS_Domain;
using ERS_Domain.CAService;
using ERS_Domain.Exceptions;
using IntrustCA_Winservice.Process;
using System;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;

namespace CA2_Winservice
{
    public partial class wsCA2_RemoteSigning : ServiceBase
    {
        private readonly CoreService _coreService;
        private readonly RabbitmqManager _rMQManager;
        private readonly CA2SigningService _ca2Service;

        private ScanHoSoProcess _scanHoSoProcess;
        private SignHashToKhaiProcess _signHashTKProcess;
        private SignToKhaiProcess _signToKhaiProcess;
        private SignHSBHXHProcess _signHSBHXHProcess;
        private HandleErrorProcess _handleErrorProcess;

        private CancellationTokenSource _cts;

        public wsCA2_RemoteSigning()
        {
            InitializeComponent();
            _coreService = new CoreService();
            _rMQManager = new RabbitmqManager();
            _ca2Service = new CA2SigningService();
        }

        public void ManualStart(string[] args)
        {
            OnStart(args);
        }

        public void ManualStop()
        {
            this.Stop();
        }

        protected override void OnStart(string[] args)
        {
            Utilities.logger.InfoLog("OnStart", "CA2 window service started");
            _cts = new CancellationTokenSource();
            Task.Run( () =>
            {
                try
                {
                    _scanHoSoProcess = new ScanHoSoProcess(_rMQManager.CreateChanel(), _coreService);
                    ScanHoSoWorker scanWorker = new ScanHoSoWorker(_scanHoSoProcess, _cts.Token);
                    _ = scanWorker.RunAsync();

                    _signHashTKProcess = new SignHashToKhaiProcess(_rMQManager.CreateChanel(), _coreService, _ca2Service);
                    _signHashTKProcess.StartProcess();

                    _signToKhaiProcess = new SignToKhaiProcess(_rMQManager.CreateChanel(), _coreService, _ca2Service);
                    _signToKhaiProcess.StartProcess();

                    _signHSBHXHProcess = new SignHSBHXHProcess(_rMQManager.CreateChanel(), _coreService, _ca2Service);
                    _signHSBHXHProcess.StartProcess();

                    _handleErrorProcess = new HandleErrorProcess(_rMQManager, _coreService);
                    _handleErrorProcess.StartProcess();
                }
            catch (DatabaseInteractException ex)
            {
                //luu lai cac guidHS ma update database loi
                Utilities.logger.InfoLog("Unable to update list", string.Join(",", ex.listIdError));
                Utilities.logger.ErrorLog(ex, "Error while updating database");
                RequestStop(ex.Message);
            }
            catch (Exception ex)
            {
                Utilities.logger.ErrorLog(ex, "Process");
            }});
        }

      

        private void RequestStop(string reason)
        {
            try
            {
                Utilities.logger.InfoLog("Service stopping...", $"Reason: {reason}");
                Stop();
            }
            catch (Exception ex)
            {
                Utilities.logger.ErrorLog(ex, "Error during RequestStop()");
            }
        }

        protected override void OnStop()
        {
            Utilities.logger.InfoLog("OnStop", "CA2 window service stopped");
            _cts.Cancel();
            _cts.Dispose();
        }
    }
}
