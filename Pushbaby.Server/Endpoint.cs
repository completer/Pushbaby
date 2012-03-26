using System.IO;
using System.Net;
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
        readonly IThreadManager threadManager;
        readonly IListenerFactory listenerFactory;
        readonly IHandlerFactory handlerFactory;

        public Endpoint(ILog log, EndpointSettings settings, IFileSystem fileSystem, IThreadManager threadManager, IListenerFactory listenerFactory, IHandlerFactory handlerFactory)
        {
            this.log = log;
            this.settings = settings;
            this.fileSystem = fileSystem;
            this.threadManager = threadManager;
            this.listenerFactory = listenerFactory;
            this.handlerFactory = handlerFactory;
        }

        public void Listen()
        {
            this.fileSystem.CreateDirectory(this.settings.PayloadDirectory);

            var listener = this.listenerFactory.Create();
            listener.AddPrefix(this.settings.Uri);
            listener.Start();
            this.log.InfoFormat("Endpoint listening on {0}", this.settings.Uri);

            while (true)
            {
                var context = listener.GetContext();
                var handler = this.handlerFactory.Create(this.settings, context);
                this.threadManager.Queue(handler.Handle);
            }
        }
    }
}
