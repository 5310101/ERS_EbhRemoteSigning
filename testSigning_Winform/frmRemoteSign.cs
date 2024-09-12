using System;
using System.Windows.Forms;

namespace testSigning_Winform
{
    public partial class frmRemoteSign : Form
    {
        private RemoteSign objFrm;
        public frmRemoteSign()
        {
            InitializeComponent();
            objFrm = new RemoteSign(this);  

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
    }
}
