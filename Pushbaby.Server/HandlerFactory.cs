using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace Pushbaby.Server
{
    public interface IHandlerFactory
    {
        Handler Create(HttpListenerContext context, Session session);
    }
}
