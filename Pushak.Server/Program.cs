﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using Pushak.Server.Logging;
using Pushak.Shared;
using log4net;

namespace Pushak.Server
{
    class Program
    {
        public static readonly string ListenerPath = "pushak";

        static void Main()
        {
            Log4NetConfiguration.Configure();
            var log = LogManager.GetLogger(typeof(Program));

            using (var listener = new HttpListener())
            {
                listener.Prefixes.Add("http://*:80/" + ListenerPath + "/");
                listener.Start();
                log.Info("Server is starting up...");

                var sessionFactory = new SessionFactory(log);

                while (true)
                {
                    var context = listener.GetContext();
                    var handler = new Handler(log, sessionFactory);
                    ThreadPool.QueueUserWorkItem(x => handler.Handle(context));
                }
            }
        }
    }

    public class Handler
    {
        readonly ILog log;
        readonly SessionFactory sessionFactory;

        public Handler(ILog log, SessionFactory sessionFactory)
        {
            this.log = log;
            this.sessionFactory = sessionFactory;
        }

        public void Handle(HttpListenerContext context)
        {
            var session = this.sessionFactory.Get(context);

            try
            {
                if (context.Request.HttpMethod == "POST" && context.Request.Headers["session"] == null)
                    this.HandleGreeting(context, session);
                else if (context.Request.HttpMethod == "POST")
                    this.HandlePayload(context, session);
                else
                    this.HandleProgress(context, session);
            }
            catch (Exception ex)
            {
                this.log.Error("Unhandled exception.", ex);
                context.Response.StatusCode = 500;
                this.WriteResponse("Unhandled exception. See server log.", context.Response, session.State);
            }
        }

        public void HandleGreeting(HttpListenerContext context, Session session)
        {
            this.log.InfoFormat("Handling greeting for session {0}...", session.Key);

            this.WriteResponse(session.Key, context.Response, session.State);
            session.State = State.Greeted;
        }

        public void HandlePayload(HttpListenerContext context, Session session)
        {
            this.log.InfoFormat("Handling payload for session {0}...", session.Key);

            this.SavePayload(context.Request, session);
            ThreadPool.QueueUserWorkItem(x => this.ExecuteBat(session));
            this.WriteResponse("OK", context.Response, session.State);
        }

        void SavePayload(HttpListenerRequest request, Session session)
        {
            session.State = State.Uploading;

            // todo: stream to file
            var reader = new StreamReader(request.InputStream);
            string s = reader.ReadToEnd();

            session.State = State.Uploaded;
        }

        void ExecuteBat(Session session)
        {
            session.State = State.Executing;

            using (var p = new Process { StartInfo = new ProcessStartInfo() })
            {
                p.StartInfo.FileName = "pushak.bat";
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardOutput = true;
                p.OutputDataReceived += (s, e) => session.WriteBuffer(e.Data);

                session.WriteBuffer("Running bat file...");
                p.Start();
                p.BeginOutputReadLine();
                p.WaitForExit();

                if (p.ExitCode > 0)
                {
                    session.WriteBuffer("Exited with code " + p.ExitCode);
                }
            }

            session.State = State.Executed;
        }

        public void HandleProgress(HttpListenerContext context, Session session)
        {
            this.log.InfoFormat("Handling progress for session {0}...", session.Key);

            if (session.State == State.Executing)
            {
                this.WriteResponse(session.ReadBuffer(), context.Response, session.State);
            }
            else if (session.State == State.Executed)
            {
                this.WriteResponse(session.ReadBuffer(), context.Response, session.State);
                this.sessionFactory.Remove(session);
            }
            else
            {
                throw new InvalidOperationException("Session ended");
            }
        }

        void WriteResponse(string s, HttpListenerResponse response, State state)
        {
            response.Headers.Add("state", state.ToString());

            var bytes = Encoding.UTF8.GetBytes(s);

            using (var output = response.OutputStream)
            {
                response.ContentLength64 = bytes.Length;
                output.Write(bytes, 0, bytes.Length);
            }
        }
    }

    public enum State { Greeting = 0, Greeted, Uploading, Uploaded, Executing, Executed }
}
