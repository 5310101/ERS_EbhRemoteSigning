using ERS_Domain;
using System;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;
using ws_GetResult_RemoteSigning.BackgroudWorker;
using ws_GetResult_RemoteSigning.Process;
using ws_GetResult_RemoteSigning.Utils;

namespace ws_GetResult_RemoteSigning
{
    public partial class ServiceGetResult_VNPT : ServiceBase
    {
        private SigningService _signingService;
        private readonly CoreService _coreService;
        private readonly RabbitmqManager _rabbitmqManager;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        private ScanHoSoWorker _scanHoSoWorker;

        private ScanHoSoProcess _scanHoSoProcess;
        private SignHashToKhaiProcess _signHashToKhaiProcess;
        private SignHashHSDKProcess _signHashHSDKProcess;
        private GetResultTokhaiProcess _getResultTokhaiProcess;
        private GetResultHoSoProcess _getResultHoSoProcess;
        private HandleDlqProcess _handleDlqProcess;

        public ServiceGetResult_VNPT()
        {
            InitializeComponent();
            _coreService = new CoreService();
            _signingService = new SigningService();
            _rabbitmqManager = new RabbitmqManager();
        }

        #region test method
        public void SetStart(string[] args)
        {
            this.OnStart(args);
        }

        public void SetStop()
        {
            this.OnStop();
        }
        #endregion

        protected override void OnStart(string[] args)
        {
            Utilities.logger.InfoLog("OnStart", "Smart Ca window service started");
            Task.Run(async () =>
            {
                try
                {
                    //khoi chay tung process
                    //cac process duoi chi don thuan la dang ky su kien
                    //cai scan hoso là background worker chạy theo vong lap 
                    _scanHoSoProcess = new ScanHoSoProcess(_rabbitmqManager, _coreService);
                    _scanHoSoWorker = new ScanHoSoWorker(_scanHoSoProcess);
                    await _scanHoSoProcess.DeclareInfrastructureAsync().ConfigureAwait(false);
                    _ = _scanHoSoWorker.RunAsync(_cts.Token);

                    //signhash tk
                    _signHashToKhaiProcess = new SignHashToKhaiProcess(_rabbitmqManager, _coreService, _signingService);
                    await _signHashToKhaiProcess.DeclareInfrastructureAsync().ConfigureAwait(false);
                    await _signHashToKhaiProcess.StartConsumingAsync(_cts.Token).ConfigureAwait(false);

                    //signhash HSDK
                    _signHashHSDKProcess = new SignHashHSDKProcess(_rabbitmqManager, _coreService, _signingService);
                    await _signHashHSDKProcess.StartConsumingAsync(_cts.Token).ConfigureAwait(false);

                    //Getresult ToKhai
                    _getResultTokhaiProcess = new GetResultTokhaiProcess(_rabbitmqManager, _coreService, _signingService);
                    await _getResultTokhaiProcess.DeclareInfrastructureAsync().ConfigureAwait(false);
                    await _getResultTokhaiProcess.StartConsumingAsync(_cts.Token).ConfigureAwait(false);

                    //getresult hoso
                    _getResultHoSoProcess = new GetResultHoSoProcess(_rabbitmqManager, _coreService, _signingService);
                    await _getResultHoSoProcess.StartConsumingAsync(_cts.Token).ConfigureAwait(false);

                    _handleDlqProcess = new HandleDlqProcess(_rabbitmqManager, _coreService);
                    await _handleDlqProcess.StartConsumingHandleDlqAsync(_cts.Token).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Utilities.logger.ErrorLog(ex,$"Error when start service");
                }
            });
        }

        protected override void OnStop()
        {
            Utilities.logger.InfoLog("OnStop", "Service stopped");
            _cts.Cancel();
            _cts.Dispose();
        }

    }
}
