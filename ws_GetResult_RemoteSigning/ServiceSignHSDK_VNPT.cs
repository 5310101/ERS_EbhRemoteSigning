using ERS_Domain.CAService;
using ERS_Domain.clsUtilities;
using ERS_Domain;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using ERS_Domain.Model;
using System.Configuration;
using ws_GetResult_RemoteSigning.Utils;

namespace ws_GetResult_RemoteSigning
{
    /// <summary>
    /// window service ký file hs đăng ký GDDT
    /// </summary>
    partial class ServiceSignHSDK_VNPT : ServiceBase
    {
        private readonly ServiceStore _store;
       
        //cac timer
        //timer ky cac hs dang ky ngoai tru hsdk cap ma lan dau
        private Timer _signHSDKTimer;
        private int _signHSDKInterval = int.Parse(ConfigurationManager.AppSettings["SIGNHSDK_TIME_INTERVAL"]);

        public ServiceSignHSDK_VNPT()
        {
            InitializeComponent();
           _store = new ServiceStore();
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
            Utilities.logger.InfoLog("OnStart", "Service started");
            try
            {
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

        private void SignHSDK_Elapsed(object sender, ElapsedEventArgs e)
        {
            _signHSDKTimer.Enabled = false;
            try
            {
                _store.SignHSDK_Type1();
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
