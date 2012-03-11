using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using Pushak.Server.Logging;
using Pushak.Shared;
using log4net;

namespace Pushak.Server
{
    class Program
    {
        public static readonly string ListenerPath = "pushak";

        static void Main()
        {
            Log4NetConfiguration.Configure();
            var log = LogManager.GetLogger(typeof(Program));

            using (var listener = new HttpListener())
            {
                listener.Prefixes.Add("http://*:80/" + ListenerPath + "/");
                listener.Start();
                log.Info("Server is starting up...");

                var sessionFactory = new SessionFactory(log);

                while (true)
                {
                    var context = listener.GetContext();
                    var session = sessionFactory.Get(context);
                    var handler = new Handler(log, context, session);
                    ThreadPool.QueueUserWorkItem(x => handler.Handle());
                }
            }
        }
    }

    public enum State { Greeting = 0, Greeted, Uploading, Uploaded, Executing, Executed }
}
