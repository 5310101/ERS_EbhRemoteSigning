using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace testSigning_Winform.CustomControl
{
    public partial class FileDisplayControl : UserControl
    {
        private Timer _timer;

        private int _counter = 300;

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


        public FileDisplayControl()
        {
            InitializeComponent();
            IntializeTimer();
        }

        private void IntializeTimer()
        {
            _timer = new Timer();  
            _timer.Interval = 1000;
            _timer.Tick += Timer_Tick;
        }


        private void Timer_Tick(object sender, EventArgs e)
        {
            _counter--;
            if(_counter <= 0)
            {
                lblFileTime.BackColor = Color.Red;
                lblFileTime.Text = "Timeup";
            }
            else
            {
                lblTrangThai.Text = "Wait";
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
            _timer.Dispose();
            lblFileTime.Text = "00";
            lblTrangThai.Text = "Confirmed";
        }

        public void SetFileName(string name)
        {
            lblFileTime.Text = name;    
        }

        //private void FileDisplayControl_Load(object sender, EventArgs e)
        //{

        //}

        public void SetStatusText(string text, Color color = default)
        {
            if (color == default)
            {
                color = Color.Chartreuse;
            }
            if (string.IsNullOrEmpty(text))
            {
                lblTrangThai.Text = text;
            }
            lblTrangThai.BackColor = color;
        }

    }
}
