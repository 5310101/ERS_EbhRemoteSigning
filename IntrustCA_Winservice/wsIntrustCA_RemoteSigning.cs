using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace IntrustCA_Winservice
{
    public partial class wsIntrustCA_RemoteSigning : ServiceBase
    {
        private readonly RabbitmqManager _rmqManager;

        //cac process chay song song

        public wsIntrustCA_RemoteSigning()
        {
            InitializeComponent();
            _rmqManager = new RabbitmqManager();
        }

        protected override void OnStart(string[] args)
        {

        }

        protected override void OnStop()
        {
        }
    }
}
