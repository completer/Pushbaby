using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
            this.Kernel.Bind<HandlerFactory>().ToSelf();
        }
    }

}
