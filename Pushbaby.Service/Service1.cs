using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;

namespace Pushbaby.Service
{
    public partial class PushbabyService : ServiceBase
    {
        public PushbabyService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            var worker = new Thread(Pushbaby.Server.Server.Main)
                {
                    Name = "Pushbaby.Server.ListenerThread",
                    IsBackground = false
                };

            worker.Start();
        }

        protected override void OnStop()
        {
        }
    }
}
