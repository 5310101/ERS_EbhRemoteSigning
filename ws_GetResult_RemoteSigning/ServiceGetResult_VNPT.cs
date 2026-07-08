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
        private SigningService _signingService;
        private readonly CoreService _coreService;


        public ServiceGetResult_VNPT()
        {
            InitializeComponent();
            _coreService = new CoreService();
            _signingService = new SigningService();

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
