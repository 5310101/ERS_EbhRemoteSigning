﻿using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Serialization;
using testSigning_Winform.CustomControl;
using testSigning_Winform.Response;
using VnptHashSignatures.Interface;
using VnptHashSignatures.Xml;

namespace testSigning_Winform
{
    public partial class frmRemoteSign : Form
    {
        public bool isSignedByService;
        private RemoteSign objFrm;
        private Timer _timer;
        private Timer _timerGetResult;
        private int _counterGetResult = 300;

        private IHashSigner _signer;
        private DataSign _dataSign;

        private Timer _timerCountDown;

        public frmRemoteSign()
        {
            InitializeComponent();
            objFrm = new RemoteSign(this);
            //Tai Khoan SMARTCA SDK
            objFrm.uid = "0101300842";
            InitializeTimer();

        }

        private void InitializeTimer()
        {
            _timer = new Timer();
            _timer.Interval = 10000;
            _timer.Tick += Timer_Tick;

            _timerGetResult = new Timer();
            _timerGetResult.Interval = 10000;
            _timerGetResult.Tick += TimerGetResult_Tick;

            _timerCountDown = new Timer();
            _timerCountDown.Interval = 1000;
            _timerCountDown.Tick += TimerCountDown_Tick;
        }

        private void TimerCountDown_Tick(object sender, EventArgs e)
        {
            _counterGetResult--;
            if(_counterGetResult <= 0)
            {
                lblTrangThai.Text = "File is no longer existed";
                lblTrangThai.BackColor = Color.Red;
                _timerCountDown.Stop();
            }
            else
            {
                lblTimeLeft.Text = _counterGetResult.ToString();
            }
        }

        private void TimerGetResult_Tick(object sender, EventArgs e)
        {
            GetResultBHXH();
        }

        private void GetResultBHXH()
        {
            try
            {
                string path = Path.Combine(objFrm.SignedFolderPath, objFrm.GuidHS, "BHXHDienTu.xml");
                bool isSuccess = objFrm.GetResult_Xml(_signer, _dataSign, path);
                if (!isSuccess)
                {
                    if (_counterGetResult <= 0)
                    {
                        _timerGetResult.Stop();
                        _timerCountDown.Stop();
                        _signer = null;
                        _dataSign = null;
                        lblTrangThai.Text = "File is no longer existed";
                        lblTrangThai.BackColor = Color.Red;
                    }
                    return;
                }
                _timerGetResult.Stop();
                _timerCountDown.Stop();
                _signer = null;
                _dataSign = null;
                lblTrangThai.Text = "Signed";
                lblTrangThai.BackColor = Color.Chartreuse;
                MessageBox.Show("Signed successfully","Notification");
                lblTimeLeft.Text = "00";
            }
            catch 
            {
                //log
                _timerGetResult.Stop();
                _timerCountDown.Stop();
                _signer = null;
                _dataSign = null;
            }
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            GetResult();
        }

        private void GetResult()
        {
            try
            {
                FileDisplayControl[] controls = this.panelToKhai.Controls.OfType<FileDisplayControl>().ToArray();
                if (controls != null)
                {
                    objFrm.GetResult_ToKhai(controls, objFrm.GuidHS);
                }
                var listbool = controls.Select(c => c.isSigned);
                //FAIL
                if (listbool.Contains(false))
                {
                    bool cntLeft = controls.Any(r => r.CheckTime());
                    if (!cntLeft)
                    {
                        _timer.Stop();
                    }
                }
                //SUCCESS
                else
                {
                    _timer.Stop();
                }
            }
            catch 
            {
                //Ghilog ex
                _timer.Stop();
            }
        }

        private void btnChonFolder_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Select Folder";
                dialog.ShowNewFolderButton = true;
                if(dialog.ShowDialog() == DialogResult.OK)
                {
                    txtFolder.Text = dialog.SelectedPath;
                    objFrm.LoadToKhai(txtFolder.Text);
                }
            }
        }

        private void btnKyToKhai_Click(object sender, EventArgs e)
        {
            FileDisplayControl[] controls = this.panelToKhai.Controls.OfType<FileDisplayControl>().ToArray();

            if (chkTestKyService.Checked)
            {
                objFrm.SignToKhai_Service(controls);
            }
            else
            {
                if (controls != null)
                {
                    objFrm.SignToKhai(controls);
                    _timer.Start();
                }
            } 
        }

        private void btnLayKQTK_Click(object sender, EventArgs e)
        {
            try
            {
                FileDisplayControl[] controls = this.panelToKhai.Controls.OfType<FileDisplayControl>().ToArray();
                if (controls != null)
                {
                    objFrm.GetResult_ToKhai(controls, objFrm.GuidHS);
                }
                var listbool = controls.Select(c => c.isSigned);
                if (!listbool.Contains(false))
                {
                    MessageBox.Show("All document has been signed");
                }
            }
            catch 
            {
                //Ghilog
            }
        }

        private void btnTestFolder_Click(object sender, EventArgs e)
        {
            try
            {
                //string path = "C:\\Users\\quanna\\Desktop\\testapi_smartca\\TestResult";
                //DirectoryInfo di = new DirectoryInfo(path);
                //DirectorySecurity ds = di.GetAccessControl();
                //AuthorizationRuleCollection rules = ds.GetAccessRules(true, true, typeof(NTAccount));
                var control = panelToKhai.Controls.OfType<FileDisplayControl>().First();
                //string json = JsonConvert.SerializeObject(control.signer);
                //File.WriteAllText("C:\\Users\\quanna\\Desktop\\testapi_smartca\\SerializeObject\\Signer.txt", json);

                XmlSerializer serializer = new XmlSerializer(typeof(XmlHashSigner));
                using (var writer = new StreamWriter("C:\\Users\\quanna\\Desktop\\testapi_smartca\\SerializeObject\\Signer.xml"))
                {
                    serializer.Serialize(writer, control.signer);
                }
            }
            catch (Exception)
            {

                throw;
            }
        }

        private bool CheckStatusFile()
        {
            foreach (var control in panelToKhai.Controls.OfType<FileDisplayControl>())
            {
                if (!control.isSigned)
                {
                    return false;
                }
            }
            return true;
        }


        private void button2_Click(object sender, EventArgs e)
        {
            try
            {
                //check tung to khai da ky chua
                bool isCheck = CheckStatusFile();
                if (!isCheck) { return; }
                //string GuidHS = "f10a83ca-ef78-414b-bf93-7bf0df265cca";
                //objFrm.GuidHS = GuidHS;
                //lblGuidHS.Text = GuidHS;
                bool isSuccess = objFrm.CreateBHXHDienTu(objFrm.GuidHS);
                if (!isSuccess)
                {
                    MessageBox.Show("Cannot create file BHXHDienTu.xml");
                    return;
                }

                //XmlDocument doc = new XmlDocument();
                //string pathBHXH = Path.Combine(objFrm.SignedFolderPath, objFrm.GuidHS, "BHXHDienTu.xml");
                //doc.Load(pathBHXH);
                //XmlNode node = doc.SelectSingleNode("//CKy_Dvi");
                //if (node == null)
                //{
                //    MessageBox.Show("Cannot find sign node");
                //}

                //Tien hanh ky file BHXHDienTu
                IHashSigner signer;
                DataSign result = objFrm.SignFileBHXH(out signer);
                if (result == null || signer == null)
                {
                    MessageBox.Show("Cannot sign file");
                    return;
                }
                _timerCountDown.Start();
                _timerGetResult.Start();
                _signer = signer;
                _dataSign = result;
            }
            catch 
            {
                //log
                return;
            }
            
        }

        private void btnLayKetQua_Click(object sender, EventArgs e)
        {
            GetResultBHXH();
        }

        private void btnThoat_Click(object sender, EventArgs e)
        {
            ResetAll();
        }

        private void ResetAll()
        {
            DialogResult dl = MessageBox.Show("Cancel Signing current file and start new process?", "Confirm", MessageBoxButtons.YesNo);
            if (dl == DialogResult.Yes) 
            {
                _timer.Stop();
                _timerCountDown.Stop();
                _timerGetResult.Stop();
                lblGuidHS.Text = "GUID Hồ sơ";
                lblGuidHS.BackColor = Color.Chartreuse;
                lblTrangThai.Text = "Trạng thái hồ sơ trên server SMART CA";
                lblTrangThai.BackColor = Color.Chartreuse;
                lblTimeLeft.Text = "300";
                _signer = null;
                _dataSign = null;
                objFrm.GuidHS = null;
                panelToKhai.Controls.Clear();
            }
        }

        private void chkTestKyService_CheckedChanged(object sender, EventArgs e)
        {
            if(chkTestKyService.Checked == true)
                isSignedByService = true;   
            else isSignedByService = false;
        }
    }
}
