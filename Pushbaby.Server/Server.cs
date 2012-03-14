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
        readonly IRouterFactory routerFactory;

        public Server(ILog log, Settings settings, IRouterFactory routerFactory)
        {
            this.log = log;
            this.settings = settings;
            this.routerFactory = routerFactory;
        }

        public void Run()
        {
            this.log.Info("Server is starting up...");

            Directory.CreateDirectory(settings.DeploymentDirectory);

            using (var listener = new HttpListener())
            {
                string uriPrefix = settings.UriPrefix ?? "http://+:80/pushbaby/";

                listener.Prefixes.Add(uriPrefix);
                listener.Start();
                this.log.InfoFormat("Server has started up. Listening on {0}", uriPrefix);

                while (true)
                {
                    var context = listener.GetContext();
                    var router = this.routerFactory.Create(context);
                    ThreadPool.QueueUserWorkItem(x => router.Route());
                }
            }
        }
    }

    public enum State { Greeting = 0, Greeted, Uploading, Uploaded, Executing, Executed }
}
