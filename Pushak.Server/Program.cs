using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;

namespace Pushak.Server
{
    class Program
    {
        public static readonly string ListenerPath = "pushak";

        static void Main()
        {
            using (var listener = new HttpListener())
            {
                listener.Prefixes.Add("http://*:80/" + ListenerPath + "/");
                listener.Start();
                Console.WriteLine("Listening...");

                while (true)
                {
                    var context = listener.GetContext(); // blocks
                    var handler = new Handler();
                    ThreadPool.QueueUserWorkItem(x => handler.Handle(context));
                }
            }
        }
    }

    public class Handler
    {
        public void Handle(HttpListenerContext context)
        {
            var session = new SessionFactory().Get(context);

            try
            {
                if (context.Request.HttpMethod == "POST")
                    this.HandlePost(context, session);
                else
                    this.HandleGet(context, session);
            }
            catch(Exception ex)
            {
                context.Response.StatusCode = 500;
                this.WriteResponse(ex.ToString(), context.Response, session.State);
            }
        }

        public class SessionFactory
        {
            static readonly Session Session = new Session(); // todo

            public Session Get(HttpListenerContext context)
            {
                return Session;
            }
        }

        public void HandlePost(HttpListenerContext context, Session session)
        {
            Console.WriteLine("Handling POST...");

            // save payload
            this.SavePayload(context.Request, session);

            // run the bat file
            ThreadPool.QueueUserWorkItem(x => this.ExecuteBat(session));

            // return ok
            this.WriteResponse("OK", context.Response, session.State);
        }

        void SavePayload(HttpListenerRequest request, Session session)
        {
            session.State = State.Uploading;

            // todo: stream to feile
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

        public void HandleGet(HttpListenerContext context, Session session)
        {
            Console.WriteLine("Handling GET...");

            if (session.State == State.Executing)
            {
                this.WriteResponse(session.ReadBuffer(), context.Response, session.State);
            }
            else if (session.State == State.Executed)
            {
                this.WriteResponse(session.ReadBuffer(), context.Response, session.State);
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

    public class Session
    {
        readonly ConcurrentQueue<string> buffer = new ConcurrentQueue<string>();

        public State State { get; set; }

        public void WriteBuffer(string s)
        {
            this.buffer.Enqueue("Pushak.Server:: " + s);
        }

        public string ReadBuffer()
        {
            var ss = new List<string>();
            string s;
            while (buffer.TryDequeue(out s)) { ss.Add(s); }
            return String.Join(Environment.NewLine, ss);
        }
    }

    public enum State { New = 0, Uploading, Uploaded, Executing, Executed }
}
