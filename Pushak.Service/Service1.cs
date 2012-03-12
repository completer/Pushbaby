using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;

namespace Pushak.Service
{
    public partial class PushakService : ServiceBase
    {
        public PushakService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            var worker = new Thread(Pushak.Server.Server.Main)
                {
                    Name = "Pushak.Server.ListenerThread",
                    IsBackground = false
                };

            worker.Start();
        }

        protected override void OnStop()
        {
        }
    }
}
