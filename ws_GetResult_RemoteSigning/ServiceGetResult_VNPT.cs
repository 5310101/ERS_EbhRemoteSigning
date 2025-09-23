using ERS_Domain;
using System;
using System.Configuration;
using System.ServiceProcess;
using System.Timers;
using ws_GetResult_RemoteSigning.Utils;

namespace ws_GetResult_RemoteSigning
{
    public partial class ServiceGetResult_VNPT : ServiceBase
    {
        private SigningService _service;

        #region Timer ky to khai
        private Timer _signTKTimer;
        //thoi gian chay tu dong cua timer co the dieu chinh, mac dinh la 0.1s
        private int _signtkTimeInterval = int.Parse(ConfigurationManager.AppSettings["SIGNTK_TIME_INTERVAL"]);
        #endregion

        #region Timer Lay ket qua to khai
        private Timer _getResultTKTimer;
        //thoi gian chay tu dong cua timer co the dieu chinh, mac dinh la 0.1s
        private int _tkTimeInterval = int.Parse(ConfigurationManager.AppSettings["TK_TIME_INTERVAL"]);
        #endregion

        #region Timer ky hash file BHXHDienTu.xml
        private Timer _signHSTimer;
        //thoi gian chay tu dong cua timer co the dieu chinh, mac dinh la 0.1s
        private int _signHSTimeInterval = int.Parse(ConfigurationManager.AppSettings["SIGNHS_TIME_INTERVAL"]);
        #endregion

        #region timer lay ket qua ho so
        private Timer _getResultHSTimer;
        //thoi gian chay tu dong cua timer co the dieu chinh, mac dinh la 0.1s
        private int _hsTimeInterval = int.Parse(ConfigurationManager.AppSettings["HS_TIME_INTERVAL"]);
        #endregion

        //timer ky cac hs dang ky (ngoai tru hsdk cap ma lan dau)
        #region timer sign ho so dang ky (tru dk cap ma lan dau)
        private Timer _signHSDKTimer;
        private int _signHSDKInterval = int.Parse(ConfigurationManager.AppSettings["SIGNHSDK_TIME_INTERVAL"]);
        #endregion

        public ServiceGetResult_VNPT()
        {
            InitializeComponent();

        }

        #region test method
        public void SetStart(string[] args)
        {
            this.OnStart(args);
        }

        public void SetStop()
        {
            _getResultTKTimer.Enabled = false;
            this.OnStop();
        }
        #endregion

        protected override void OnStart(string[] args)
        {
            Utilities.logger.InfoLog("OnStart", "Service started");
            _service = new SigningService();
            try
            {
                _signTKTimer = new Timer();
                _signTKTimer.Interval = _signtkTimeInterval;
                _signTKTimer.AutoReset = true;
                _signTKTimer.Elapsed += SignTKTimer_Elapsed;
                _signTKTimer.Enabled = true;

                _getResultTKTimer = new Timer();
                _getResultTKTimer.Interval = _tkTimeInterval;
                _getResultTKTimer.AutoReset = true;
                _getResultTKTimer.Elapsed += TKTimer_Elapsed;
                _getResultTKTimer.Enabled = true;

                _signHSTimer = new Timer();
                _signHSTimer.Interval = _signHSTimeInterval;
                _signHSTimer.AutoReset = true;
                _signHSTimer.Elapsed += SignHSTimer_Elapsed;
                _signHSTimer.Enabled = true;

                _getResultHSTimer = new Timer();
                _getResultHSTimer.Interval = _hsTimeInterval;
                _getResultHSTimer.AutoReset = true;
                _getResultHSTimer.Elapsed += HSTimer_Elapsed;
                _getResultHSTimer.Enabled = true;

                _signHSDKTimer = new Timer();
                _signHSDKTimer.Interval = _signHSDKInterval;
                _signHSDKTimer.AutoReset = true;
                _signHSDKTimer.Elapsed += SignHSDK_Elapsed;
                _signHSDKTimer.Enabled = true;
            }
            catch (Exception ex)
            {
                Utilities.logger.ErrorLog(ex, "OnStart");
                this.Stop();
            }
        }

        protected override void OnStop()
        {
            Utilities.logger.InfoLog("OnStop", "Service stopped");
        }
        private void SignTKTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            _signTKTimer.Enabled = false;
            try
            {
                _service.SignToKhai_VNPT();
            }
            catch (Exception ex)
            {
                Utilities.logger.ErrorLog(ex, "SignTKTimer_Elapsed");
            }
            finally
            {
                _signTKTimer.Enabled = true;
            }
        }

        private void TKTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            _getResultTKTimer.Enabled = false;
            try
            {
                _service.GetResultToKhai_VNPT();
            }
            catch (Exception ex)
            {
                Utilities.logger.ErrorLog(ex, "TKTimer_Elapsed");
            }
            finally
            {
                _getResultTKTimer.Enabled = true;
            }
        }

        private void SignHSTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            _signHSTimer.Enabled = false;
            try
            {
                _service.SignFileBHXHDienTu();
            }
            catch (Exception ex)
            {
                Utilities.logger.ErrorLog(ex, "SignHSTimer_Elapsed");
            }
            finally
            {
                _signHSTimer.Enabled = true;
            }
        }

        private void HSTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            _getResultHSTimer.Enabled = false;
            try
            {
                _service.GetResultHoSo_VNPT();
            }
            catch (Exception ex)
            {
                Utilities.logger.ErrorLog(ex, "HSTimer_Elapsed");
            }
            finally
            {
                _getResultHSTimer.Enabled = true;
            }
        }

        private void SignHSDK_Elapsed(object sender, ElapsedEventArgs e)
        {
            _signHSDKTimer.Enabled = false;
            try
            {
                _service.SignHSDK_Type1();
            }
            catch (Exception ex)
            {
                Utilities.logger.ErrorLog(ex, "SignHSDK_Elapsed");

            }
            finally
            {
                _signHSDKTimer.Enabled = true;
            }
        }
    }
}
