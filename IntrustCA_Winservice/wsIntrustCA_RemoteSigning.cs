using ERS_Domain;
using IntrustCA_Winservice.Process;
using IntrustCA_Winservice.Services;
using System;
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

        public wsIntrustCA_RemoteSigning()
        {
            InitializeComponent();
           
        }

        protected override void OnStart(string[] args)
        {
            Utilities.logger.InfoLog("OnStart","IntrustCA window service started");
            try
            {
                _rmqManager = new RabbitmqManager();
                _coreService = new CoreService();

                //khoi tao process scan hs
                _processScanHS = new ScanHoSoProcess(_rmqManager.CreateChanel(), _coreService);
                _timer1 = new Timer();
                _timer1.Interval = 100;
                _timer1.Elapsed += ScanHS_Elapsed;
                _timer1.Enabled = true;

                //khoi tao process 

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

        private void ScanHS_Elapsed(object sender, ElapsedEventArgs e)
        {
            _timer1.Enabled = false;
            _processScanHS.Dowork();
            _timer1.Enabled = true;
        }

        protected override void OnStop()
        {
            Utilities.logger.InfoLog("OnStop", "IntrustCA window service stopped");
        }
    }
}
