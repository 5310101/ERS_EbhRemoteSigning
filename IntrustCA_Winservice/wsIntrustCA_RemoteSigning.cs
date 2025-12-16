using ERS_Domain;
using ERS_Domain.Exceptions;
using IntrustCA_Winservice.Process;
using IntrustCA_Winservice.Services;
using IntrustCA_Winservice.Worker;
using System;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace IntrustCA_Winservice
{
    public partial class wsIntrustCA_RemoteSigning : ServiceBase
    {
        private RabbitmqManager _rmqManager;
        private CoreService _coreService;

        //cac process chay song song
        //1 scan ho so day vao queue
        private ScanHoSoProcess _processScanHS;

        //2 process phan loai ho so de chuan bi ky so
        private CheckHSProcess _processCheckHS;

        //3 process tao session ky so
        private CreateSessionStoreProcess _processCreateSession;

        //4 process ky so
        private SignHSProcess _processSignHS;

        //5 proceess xu ly dead letter queue
        private HandleErrorProcess _processHandleError;

        private CancellationTokenSource _cts;

        public wsIntrustCA_RemoteSigning()
        {
            InitializeComponent();
            _rmqManager = new RabbitmqManager();
            _coreService = new CoreService();
        }

        protected override void OnStart(string[] args)
        {
            Utilities.logger.InfoLog("OnStart", "IntrustCA window service started");
            _cts = new CancellationTokenSource();
            Task.Run(() =>
            {
                try
                {
                    //khoi tao process scan hs
                    //moi publisher, consumer dùng 1 channel rieng
                    _processScanHS = new ScanHoSoProcess(_rmqManager.CreateChanel(), _coreService);
                    ScanHoSoWorker worker = new ScanHoSoWorker(_processScanHS ,_cts.Token);
                    _ = worker.RunAsync();
                    //khoi tao process phan loai ho so
                    _processCheckHS = new CheckHSProcess(_rmqManager.CreateChanel(), _coreService);
                    _processCheckHS.StartProcess();
                    //khoi tao process tao session ky so
                    _processCreateSession = new CreateSessionStoreProcess(_rmqManager.CreateChanel(), _coreService);
                    _processCreateSession.StartProcess();
                    //khoi tao process ky so
                    _processSignHS = new SignHSProcess(_rmqManager.CreateChanel(), _coreService);
                    _processSignHS.StartProcess();
                    //khoi tao process xu ly dead letter queue
                    _processHandleError = new HandleErrorProcess(_rmqManager, _coreService);
                    _processHandleError.StartProcess();
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
                    Utilities.logger.ErrorLog(ex, "OnStart");
                    this.Stop();
                }
            });
        }


        public void StartManual(string[] args)
        {
            this.OnStart(args);
        }

        public void StopManual()
        {
            this.Stop();
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
            Utilities.logger.InfoLog("OnStop", "IntrustCA window service stopped");
            _cts.Cancel();
            _cts.Dispose();
        }
    }
}
