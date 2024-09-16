using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using testSigning_Winform.Response;
using VnptHashSignatures.Interface;

namespace testSigning_Winform.CustomControl
{
    public partial class FileDisplayControl : UserControl
    {
        private Timer _timer;

        private int _counter = 300;
        private FileInfo _fileDetail;
        public readonly int index;
        public IHashSigner signer;
        public DataSign dataSign;
        public bool isSigned =false;

        public FileInfo FileDetail 
        {
            get
            {
                return _fileDetail;
            }
        }

        public int Counter 
        {
            set
            {
                _counter = value;   
            }
        }
        public string StatusText
        {
            get 
            { 
                return lblTrangThai.Text; 
            }
        }


        public FileDisplayControl(FileInfo File, int index)
        {
            InitializeComponent();
            SetFileName(File.Name);
            _fileDetail = File; 
            this.index = index;
            SetStatusText("Added");
            InitializeTimer();
        }

        private void InitializeTimer()
        {
            _timer = new Timer();  
            _timer.Interval = 1000;
            _timer.Tick += Timer_Tick;
            lblFileTime.Text = "Time limit";
        }

        

        private void Timer_Tick(object sender, EventArgs e)
        {
            _counter--;
            if(_counter <= 0)
            {
                lblFileTime.BackColor = Color.Red;
                lblFileTime.Text = "Failed";
                _timer.Stop();
            }
            else
            {
                lblFileTime.Text = _counter.ToString();
            }
        }

        public void StartCountDown(int counter)
        {
            _counter = counter;
            lblFileTime.Text = counter.ToString();  
            _timer.Start();
        } 

        public void SetSuccess()
        {
            _timer.Stop();
            lblFileTime.Text = "00";
            isSigned = true;
            lblTrangThai.Text = "Confirmed";
            lblTrangThai.BackColor = Color.Chartreuse;
        }

        public void SetNameControl(string name)
        {
            this.Name = name;   
        }

        public void SetFileName(string name)
        {
            txtFileName.Text = name;
            txtFileName.ReadOnly = true;
        }

        //private void FileDisplayControl_Load(object sender, EventArgs e)
        //{

        //}

        public void SetSigner(IHashSigner signer)
        {
            this.signer = signer;   
        }

        public void SetDataSign(DataSign dataSign)
        {
            this.dataSign = dataSign;
        }

        public void SetStatusText(string text, Color color = default)
        {
            if (color == default)
            {
                color = Color.Chartreuse;
            }
            if (!string.IsNullOrEmpty(text))
            {
                lblTrangThai.Text = text;
            }
            lblTrangThai.BackColor = color;
        }

        public bool CheckTime()
        {
            if(_counter > 0)
            {
                return true;
            }
            return false;
        }
    }
}
