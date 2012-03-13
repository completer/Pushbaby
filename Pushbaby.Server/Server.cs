using System;
using System.IO;
using System.Net;
using System.Threading;
using Pushbaby.Server.Logging;
using log4net;

namespace Pushbaby.Server
{
    public class Server
    {
        public static readonly string ListenerPath = "";

        public static void Main()
        {
            Log4NetConfiguration.Configure();
            var log = LogManager.GetLogger(typeof(Server));

            var settings = new Settings();
            settings.Validate();

            Directory.CreateDirectory(settings.DeploymentDirectory);

            using (var listener = new HttpListener())
            {
                listener.Prefixes.Add(settings.UriPrefix ?? "http://+:80/pushak/");
                listener.Start();
                log.Info("Server is starting up...");

                var sessionFactory = new SessionFactory(log);

                while (true)
                {
                    var context = listener.GetContext();
                    var handler = new Handler(log, settings, context, sessionFactory);
                    ThreadPool.QueueUserWorkItem(x => handler.Handle());
                }
            }
        }
    }

    public enum State { Greeting = 0, Greeted, Uploading, Uploaded, Executing, Executed }
}
