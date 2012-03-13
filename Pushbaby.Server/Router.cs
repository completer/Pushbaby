using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Pushbaby.Shared;
using log4net;

namespace Pushbaby.Server
{
    public class Router
    {
        readonly ILog log;
        readonly HttpListenerContext context;
        readonly SessionManager sessionManager;
        readonly IHandlerFactory handlerFactory;

        public Router(ILog log, HttpListenerContext context, SessionManager sessionManager, IHandlerFactory handlerFactory)
        {
            this.log = log;
            this.context = context;
            this.sessionManager = sessionManager;
            this.handlerFactory = handlerFactory;
        }

        public void Route()
        {
            try
            {
                var session = this.sessionManager.Get(this.context);
                var handler = this.handlerFactory.Create(this.context, session);

                if (this.context.Request.Headers["session"] == null)
                {
                    if (this.context.Request.HttpMethod == "POST")
                        handler.HandleGreeting();
                    else
                        handler.HandleHomepage();
                }
                else
                {
                    if (this.context.Request.HttpMethod == "POST")
                        handler.HandlePayload();
                    else
                        handler.HandleProgress();
                }

                this.sessionManager.Put(session);
            }
            catch (Exception ex)
            {
                this.log.ErrorFormat("Unhandled exception. Request: {0}. Exception: {1}", context.Request.Url, ex);

                this.context.Response.StatusCode = 500;

                using (var writer = new StreamWriter(this.context.Response.OutputStream))
                {
                    writer.Write("Pushbaby.Server:: Unhandled exception. See server log.");
                }
            }
        }
    }
}
