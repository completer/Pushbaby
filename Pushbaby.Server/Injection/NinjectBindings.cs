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
            this.Kernel.Bind<SessionManager>().ToSelf().InSingletonScope();
            this.Kernel.Bind<IRouterFactory>().ToFactory();
            this.Kernel.Bind<IHandlerFactory>().ToFactory();
        }
    }
}
