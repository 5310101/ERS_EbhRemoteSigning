using ERS_Domain;
using ERS_Domain.Exceptions;
using IntrustCA_Winservice.Process;
using IntrustCA_Winservice.Services;
using System;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Timers;

namespace IntrustCA_Winservice
{
    public partial class wsIntrustCA_RemoteSigning : ServiceBase
    {
        private RabbitmqManager _rmqManager;
        private CoreService _coreService;

        //cac process chay song song
        //1 scan ho so day vao queue
        private Timer _timer1;
        private ScanHoSoProcess _processScanHS;

        //2 process phan loai ho so de chuan bi ky so
        private Timer _timer2;
        private CheckHSProcess _processCheckHS;

        //3 process tao session ky so
        private Timer _timer3;
        private CreateSessionStoreProcess _processCreateSession;

        //4 process ky so
        private Timer _timer4;
        private SignHSProcess _processSignHS;

        //5 proceess xu ly dead letter queue
        private Timer _timer5;
        private HandleErrorProcess _processHandleError;

        //force stop
        private volatile bool _forceStop = false;

        public wsIntrustCA_RemoteSigning()
        {
            InitializeComponent();

        }

        protected override void OnStart(string[] args)
        {
            Utilities.logger.InfoLog("OnStart", "IntrustCA window service started");
            try
            {
                _rmqManager = new RabbitmqManager();
                _coreService = new CoreService();

                //khoi tao process scan hs
                //moi publisher, consumer dùng 1 channel rieng
                _processScanHS = new ScanHoSoProcess(_rmqManager.CreateChanel(), _coreService);
                _timer1 = new Timer();
                _timer1.Interval = 100;
                _timer1.Elapsed += GenerateHandler(_timer1, _processScanHS.DoWork);
                _timer1.Enabled = true;

                //khoi tao process phan loai ho so
                _processCheckHS = new CheckHSProcess(_rmqManager.CreateChanel(), _coreService);
                _timer2 = new Timer();
                _timer2.Interval = 100;
                _timer2.Elapsed += GenerateHandler(_timer2, _processCheckHS.DoWork); ;
                _timer2.Enabled = true;

                //khoi tao process tao session ky so
                _processCreateSession = new CreateSessionStoreProcess(_rmqManager.CreateChanel(), _coreService);
                _timer3 = new Timer();
                _timer3.Interval = 100;
                _timer3.Elapsed += GenerateHandler(_timer3, _processCreateSession.DoWork); ;
                _timer3.Enabled = true;

                //khoi tao process ky so
                _processSignHS = new SignHSProcess(_rmqManager.CreateChanel(), _coreService);
                _timer4 = new Timer();
                _timer4.Interval = 100;
                _timer4.Elapsed += GenerateHandler(_timer4, _processSignHS.DoWork);
                _timer4.Enabled = true;

                //khoi tao process xu ly dead letter queue
                _processHandleError = new HandleErrorProcess(_rmqManager, _coreService);
                _timer5 = new Timer();
                _timer5.Interval = 100;
                _timer5.Elapsed += GenerateHandler(_timer5, _processHandleError.DoWork);
                _timer5.Enabled = true;
            }
            catch (Exception ex)
            {
                Utilities.logger.ErrorLog(ex, "OnStart");
                this.Stop();
            }
        }


        public void StartManual(string[] args)
        {
            this.OnStart(args);
        }

        public void StopManual()
        {
            this.Stop();
        }

        public ElapsedEventHandler GenerateHandler(Timer timer, Action Dowork)
        {
            return (sender, e) =>
            {
                timer.Enabled = false;
                try
                {
                    if (_forceStop) return;
                    Dowork();
                }
                catch (DatabaseInteractException ex)
                {
                    //luu lai cac guidHS ma update database loi
                    Utilities.logger.InfoLog("Unable to update list", string.Join(",", ex.listIdError));
                    Utilities.logger.ErrorLog(ex, "Error while updating database");
                    _forceStop = true;
                    RequestStop(ex.Message);
                }
                catch (Exception ex)
                {
                    Utilities.logger.ErrorLog(ex, "OnStart");
                }
                finally
                {
                    if (!_forceStop)
                        timer.Enabled = true;
                }
            };
        }

        private void RequestStop(string reason)
        {
            try
            {
                Utilities.logger.InfoLog("Service stopping...", $"Reason: {reason}");
                this._timer1.Enabled = false;
                this._timer2.Enabled = false;
                this._timer3.Enabled = false;
                this._timer4.Enabled = false;
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
        }
    }
}
