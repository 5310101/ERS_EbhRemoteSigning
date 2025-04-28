using ERS_Domain;
using ERS_Domain.CAService;
using ERS_Domain.clsUtilities;
using ERS_Domain.CustomSigner;
using ERS_Domain.Exceptions;
using ERS_Domain.Model;
using ERS_Domain.Response;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Timers;
using System.Xml;
using System.Xml.Serialization;
using VnptHashSignatures.Common;
using VnptHashSignatures.Interface;
using VnptHashSignatures.Office;
using VnptHashSignatures.Pdf;
using ws_GetResult_RemoteSigning.Utils;

namespace ws_GetResult_RemoteSigning
{
    public partial class ServiceGetResult_VNPT : ServiceBase
    {
        private readonly ServiceStore _store;

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

        public ServiceGetResult_VNPT()
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
            _getResultTKTimer.Enabled = false;
            this.OnStop();
        }
        #endregion

        protected override void OnStart(string[] args)
        {
            Utilities.logger.InfoLog("OnStart", "Service started");
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
                _store.SignToKhai_VNPT();
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
                _store.GetResultToKhai_VNPT();
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
                _store.SignFileBHXHDienTu();
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
                _store.GetResultHoSo_VNPT();
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
    }
}
