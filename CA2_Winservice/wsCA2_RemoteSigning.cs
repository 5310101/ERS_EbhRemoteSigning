using CA2_Winservice.Process;
using CA2_Winservice.Services;
using ERS_Domain;
using ERS_Domain.CAService;
using ERS_Domain.Exceptions;
using IntrustCA_Winservice.Process;
using System;
using System.ServiceProcess;
using System.Timers;

namespace CA2_Winservice
{
    public partial class wsCA2_RemoteSigning : ServiceBase
    {
        private readonly CoreService _coreService;
        private readonly RabbitmqManager _rMQManager;
        private readonly CA2SigningService _ca2Service;

        private bool _forceStop = false;

        private ScanHoSoProcess _scanHoSoProcess;
        private Timer _timer1;

        private SignHashToKhaiProcess _signHashTKProcess;
        private Timer _timer2;

        private SignToKhaiProcess _signToKhaiProcess;
        private Timer _timer3;

        private SignHSBHXHProcess _signHSBHXHProcess;
        private Timer _timer4;

        private HandleErrorProcess _handleErrorProcess;
        private Timer _timer5;

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
            try
            {
                _scanHoSoProcess = new ScanHoSoProcess(_rMQManager.CreateChanel(), _coreService);   
                _timer1 = new Timer();
                _timer1.Interval = 100;
                _timer1.Elapsed += GenerateHandler(_timer1, () => _scanHoSoProcess.DoWork());
                _timer1.Enabled = true;

                _signHashTKProcess = new SignHashToKhaiProcess(_rMQManager.CreateChanel(), _coreService, _ca2Service);
                _timer2 = new Timer();
                _timer2.Interval = 100;
                _timer2.Elapsed += GenerateHandler(_timer2, () => _signHashTKProcess.DoWork());
                _timer2.Enabled = true;

                _signToKhaiProcess = new SignToKhaiProcess(_rMQManager.CreateChanel(), _coreService, _ca2Service);
                _timer3 = new Timer();
                _timer3.Interval = 100;
                _timer3.Elapsed += GenerateHandler(_timer3, () => _signToKhaiProcess.DoWork());
                _timer3.Enabled = true;

                _signHSBHXHProcess = new SignHSBHXHProcess(_rMQManager.CreateChanel(), _coreService, _ca2Service);
                _timer4 = new Timer();
                _timer4.Interval = 100;
                _timer4.Elapsed += GenerateHandler(_timer4, () => _signHSBHXHProcess.DoWork());
                _timer4.Enabled = true;

                _handleErrorProcess = new HandleErrorProcess(_rMQManager, _coreService);
                _timer5 = new Timer();
                _timer5.Interval = 100;
                _timer5.Elapsed += GenerateHandler(_timer5, () => _handleErrorProcess.DoWork());
                _timer5.Enabled = true;

            }
            catch (Exception ex)
            {
                Utilities.logger.ErrorLog(ex, "OnStart");
                this.Stop();
            }

        }

        private ElapsedEventHandler GenerateHandler(Timer timer, Action DoWork)
        {
            return (s, e) =>
            {
                timer.Enabled = false;
                try
                {
                    if (_forceStop) return;
                    DoWork?.Invoke();

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
                    Utilities.logger.ErrorLog(ex, "Process");
                }
                finally
                {
                    if (!_forceStop)
                    {
                        timer.Enabled = true;
                    }
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
            Utilities.logger.InfoLog("OnStop", "CA2 window service stopped");
        }


    }
}
