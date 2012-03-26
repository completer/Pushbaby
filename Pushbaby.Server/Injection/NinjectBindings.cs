using Ninject.Extensions.Factory;
using Ninject.Modules;
using log4net;

namespace Pushbaby.Server.Injection
{
    /// <summary>
    /// Defines the type bindings for dependency injection.
    /// </summary>
    public class NinjectBindings : NinjectModule
    {
        public override void Load()
        {
            this.Kernel.Bind<ILog>().ToMethod(x => LogManager.GetLogger(typeof(Server)));
            this.Kernel.Bind<IFileSystem>().To<FileSystem>();
            this.Kernel.Bind<IThreadManager>().To<ThreadManager>();
            this.Kernel.Bind<ISessionManager>().To<SessionManager>().InSingletonScope();
            this.Kernel.Bind<IEndpointFactory>().ToFactory();
            this.Kernel.Bind<IListenerFactory>().ToFactory();
            this.Kernel.Bind<IHandlerFactory>().ToFactory();
        }
    }
}
