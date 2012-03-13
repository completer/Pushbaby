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
    public interface IDispatcherFactory
    {
        
    }
    public class Dispatcher
    {
        readonly ILog log;
        readonly HttpListenerContext context;
        readonly SessionManager sessionManager;
        readonly HandlerFactory handlerFactory;

        public Dispatcher(ILog log, HttpListenerContext context, SessionManager sessionManager, HandlerFactory handlerFactory)
        {
            this.log = log;
            this.context = context;
            this.sessionManager = sessionManager;
            this.handlerFactory = handlerFactory;
        }

        public void Dispatch()
        {
            try
            {
                var session = sessionManager.Get(context);
                var handler = handlerFactory.Create(session, context);

                if (context.Request.Headers["session"] == null)
                {
                    if (context.Request.HttpMethod == "POST")
                        handler.HandleGreeting();
                    else
                        handler.HandleHomepage();
                }
                else
                {
                    if (context.Request.HttpMethod == "POST")
                        handler.HandlePayload();
                    else
                        handler.HandleProgress();
                }

                sessionManager.Put(session);
            }
            catch (Exception ex)
            {
                this.log.ErrorFormat("Unhandled exception. Request: {0}. Exception: {1}", context.Request.Url, ex);
                context.Response.StatusCode = 500;
                this.WriteResponse("Pushbaby.Server:: Unhandled exception. See server log.");
            }
        }
    }
}
