using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using log4net;

namespace Pushak.Server
{
    public class Handler
    {
        readonly ILog log;
        readonly HttpListenerContext context;
        readonly Session session;

        public Handler(ILog log, HttpListenerContext context, Session session)
        {
            this.log = log;
            this.context = context;
            this.session = session;
        }

        public void Handle()
        {
            try
            {
                if (context.Request.HttpMethod == "POST" && context.Request.Headers["session"] == null)
                    this.HandleGreeting();
                else if (context.Request.HttpMethod == "POST")
                    this.HandlePayload();
                else
                    this.HandleProgress();
            }
            catch (Exception ex)
            {
                this.log.Error("Unhandled exception.", ex);
                context.Response.StatusCode = 500;
                this.WriteResponse("Unhandled exception. See server log.");
            }
        }

        void HandleGreeting()
        {
            this.log.InfoFormat("Handling greeting for session {0}...", session.Key);

            this.WriteResponse(session.Key);
            session.State = State.Greeted;
        }

        void HandlePayload()
        {
            this.log.InfoFormat("Handling payload for session {0}...", session.Key);

            this.SavePayloadToDisk();
            ThreadPool.QueueUserWorkItem(x => this.ExecuteBatFile());
            this.WriteResponse("OK");
        }

        void HandleProgress()
        {
            this.log.InfoFormat("Handling progress for session {0}...", session.Key);

            if (session.State == State.Executing)
            {
                this.WriteResponse(session.Read());
            }
            else if (session.State == State.Executed)
            {
                this.WriteResponse(session.Read());
                this.session.Remove();
            }
            else
            {
                throw new InvalidOperationException("Invalid state for session.");
            }
        }

        void SavePayloadToDisk()
        {
            session.State = State.Uploading;

            // todo: stream to file
            var reader = new StreamReader(context.Request.InputStream);
            string s = reader.ReadToEnd();

            session.State = State.Uploaded;
        }

        void ExecuteBatFile()
        {
            session.State = State.Executing;

            using (var p = new Process { StartInfo = new ProcessStartInfo() })
            {
                p.StartInfo.FileName = "pushak.bat";
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardOutput = true;
                p.OutputDataReceived += (s, e) => session.Write(e.Data);

                session.Write("Running bat file...");
                p.Start();
                p.BeginOutputReadLine();
                p.WaitForExit();

                if (p.ExitCode > 0)
                {
                    session.Write("Exited with code " + p.ExitCode);
                }
            }

            session.State = State.Executed;
        }

        void WriteResponse(string s)
        {
            this.context.Response.Headers.Add("state", this.session.State.ToString());

            var bytes = Encoding.UTF8.GetBytes(s);

            using (var output = this.context.Response.OutputStream)
            {
                this.context.Response.ContentLength64 = bytes.Length;
                output.Write(bytes, 0, bytes.Length);
            }
        }
    }
}
