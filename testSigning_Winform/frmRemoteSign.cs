using System;
using System.Linq;
using System.Windows.Forms;
using testSigning_Winform.CustomControl;

namespace testSigning_Winform
{
    public partial class frmRemoteSign : Form
    {
        private RemoteSign objFrm;
        private Timer _timer;
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
            _timer.Enabled = true;
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            FileDisplayControl[] controls = this.panelToKhai.Controls.OfType<FileDisplayControl>().ToArray();
            if(controls != null)
            {
                objFrm.GetResult_ToKhai(controls,objFrm.GuidHS);
            }
            var listbool = controls.Select(c => c.isSigned);
            if(!listbool.Contains(false))
            {
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
            if (controls != null)
            {
                objFrm.SignToKhai(controls);
                _timer.Start();
            }
        }
    }
}
