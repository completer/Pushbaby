using System;
using log4net;

namespace Pushbaby.Server
{
    public interface IEndpoint
    {
        void Listen();
    }

    public class Endpoint : IEndpoint
    {
        readonly ILog log;
        readonly EndpointSettings settings;
        readonly IFileSystem fileSystem;
        readonly IListener listener;
        readonly IHandlerFactory handlerFactory;
        readonly IThreadManager threadManager;

        public Endpoint(ILog log, EndpointSettings settings, IFileSystem fileSystem, IListener listener, IHandlerFactory handlerFactory, IThreadManager threadManager)
        {
            this.log = log;
            this.settings = settings;
            this.fileSystem = fileSystem;
            this.listener = listener;
            this.handlerFactory = handlerFactory;
            this.threadManager = threadManager;
        }

        public void Listen()
        {
            try
            {
                this.fileSystem.CreateDirectory(this.settings.DeploymentDirectory);

                this.listener.AddPrefix(this.settings.Uri);
                this.listener.Start();
                this.log.InfoFormat("Endpoint listening on {0}", this.settings.Uri);

                while (true)
                {
                    var context = this.listener.GetContext(); // blocks until http request is received
                    var handler = this.handlerFactory.Create(this.settings, context);
                    this.threadManager.Queue(handler.Handle);
                }
            }
            catch (Exception ex)
            {
                this.log.Fatal("Unhandled exception on endpoint " + this.settings.Uri + " thread.", ex);
            }
        }
    }
}
