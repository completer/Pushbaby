using System;
using System.IO;
using System.Net;
using System.Threading;
using Ninject;
using Pushbaby.Server.Injection;
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

            var settings = new Settings();
            settings.Validate();

            var kernel = new StandardKernel();
            kernel.Load<NinjectBindings>();
            kernel.Bind<Settings>().ToConstant(settings);

            var server = kernel.Get<Server>();
            server.Run();
        }

        readonly ILog log;
        readonly Settings settings;
        readonly DispatcherFactory dispatcherFactory;

        public Server(ILog log, Settings settings, DispatcherFactory dispatcherFactory)
        {
            this.log = log;
            this.settings = settings;
            this.dispatcherFactory = dispatcherFactory;
        }

        public void Run()
        {
            Directory.CreateDirectory(settings.DeploymentDirectory);

            using (var listener = new HttpListener())
            {
                listener.Prefixes.Add(settings.UriPrefix ?? "http://+:80/pushbaby/");
                listener.Start();
                log.Info("Server is starting up...");

                while (true)
                {
                    var context = listener.GetContext();
                    var dispatcher = dispatcherFactory.Create(context);
                    ThreadPool.QueueUserWorkItem(x => dispatcher.Dispatch());
                }
            }
        }
    }

    public enum State { Greeting = 0, Greeted, Uploading, Uploaded, Executing, Executed }
}
