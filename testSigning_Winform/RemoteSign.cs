using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using testSigning_Winform.CustomControl;

namespace testSigning_Winform
{
    public class RemoteSign
    {
        private frmRemoteSign frm;
        public RemoteSign(frmRemoteSign frm)
        {
            this.frm = frm;
        }

        public void LoadToKhai(string folderPath)
        {
            string[] fileExtensions = { "*.pdf", "*.xml", "*.docx", "*.xlsx" };
            DirectoryInfo di = new DirectoryInfo(folderPath);
            List<FileInfo> listFiles = new List<FileInfo>();
            foreach (string extension in fileExtensions)
            {
               FileInfo[] fi = di.GetFiles(extension, SearchOption.AllDirectories);
                if (fi != null) 
                {
                    listFiles.AddRange(fi); 
                }
            }
            foreach (FileInfo fi in listFiles) 
            {
                FileDisplayControl fileControl = new FileDisplayControl(fi.Name);
                fileControl.SetNameControl(fi.Name);
                frm.panelToKhai.Controls.Add(fileControl);
            }
        }
    }
}
