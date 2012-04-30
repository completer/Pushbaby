using System;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using Ninject;
using Ninject.Extensions.Factory;
using Pushbaby.Server.Injection;
using Pushbaby.Server.Logging;
using log4net;

namespace Pushbaby.Server
{
    public class Server
    {
        public static void Main()
        {
            Log4NetConfiguration.Configure();

            var settings = (Settings) ConfigurationManager.GetSection("pushbaby");

            if (settings == null)
                throw new ConfigurationErrorsException("No 'pushbaby' config section was found. See documentation.");

            var kernel = new StandardKernel();
            LoadFuncModuleIfNotDebug(kernel);
            kernel.Load<NinjectBindings>();
            kernel.Bind<Settings>().ToConstant(settings);

            var server = kernel.Get<Server>();
            server.Run();
        }

        readonly ILog log;
        readonly Settings settings;
        readonly IEndpointFactory endpointFactory;
        readonly IThreadManager threadManager;

        public Server(ILog log, Settings settings, IEndpointFactory endpointFactory, IThreadManager threadManager)
        {
            this.log = log;
            this.settings = settings;
            this.endpointFactory = endpointFactory;
            this.threadManager = threadManager;
        }

        public void Run()
        {
            this.log.Info("Server is starting up...");

            try
            {
                foreach (var endpointSettings in this.settings.EndpointSettingsCollection.Cast<EndpointSettings>())
                {
                    var endpoint = this.endpointFactory.Create(endpointSettings);
                    this.threadManager.Create(endpoint.Listen);
                }

                this.log.InfoFormat("Server has started up with {0} endpoint(s).", this.settings.EndpointSettingsCollection.Count);
            }
            catch (Exception ex)
            {
                this.log.Fatal("Unhandled exception.", ex);
                throw;
            }
        }

        static void LoadFuncModuleIfNotDebug(IKernel kernel)
        {
#if !DEBUG
            // need to load this manually when using ilmerge https://groups.google.com/forum/#!msg/ninject/CUBxmWubl60/5YAwFFobO18J
            kernel.Load<FuncModule>(); 
#endif
        }
    }
}
