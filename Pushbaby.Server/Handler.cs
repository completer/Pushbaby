using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Pushbaby.Shared;
using log4net;

namespace Pushbaby.Server
{
    public class Handler
    {
        readonly ILog log;
        readonly Settings settings;
        readonly HttpListenerContext context;
        readonly Session session;

        public Handler(ILog log, Settings settings, HttpListenerContext context, Session session)
        {
            this.log = log;
            this.settings = settings;
            this.context = context;
            this.session = session;
        }

        public void HandleHomepage()
        {
            this.WriteResponse("Pushbaby.Server:: Server is running. " + DateTime.UtcNow.ToLongDateString());
        }

        public void HandleGreeting()
        {
            this.log.InfoFormat("Handling greeting for session {0}...", session.Key);

            this.WriteResponse(session.Key);
            session.State = State.Greeted;
        }

        public void HandlePayload()
        {
            this.log.InfoFormat("Handling payload for session {0}...", session.Key);

            this.SavePayloadToDisk();
            ThreadPool.QueueUserWorkItem(x => this.ExecuteBatFile());
            this.WriteResponse("OK");
        }

        public void HandleProgress()
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

            var aes = CryptoUtility.GetAlgorithm(settings.SharedSecret, this.session.Key);

            string filename = aes.DecryptString(context.Request.Headers["filename"] ?? String.Empty);
            string filenameHash = aes.DecryptString(context.Request.Headers["filename-hash"] ?? String.Empty);
            if (HashUtility.ComputeStringHash(filename) != filenameHash)
                throw new ApplicationException("Invalid filename hash.");

            string path = Path.Combine(this.settings.DeploymentDirectory, filename);

            using (var output = File.Create(path))
            using (var input = new CryptoStream(context.Request.InputStream, aes.CreateDecryptor(), CryptoStreamMode.Read))
            {
                StreamUtility.Copy(input, output);
            }

            string payloadHash = aes.DecryptString(context.Request.Headers["payload-hash"] ?? String.Empty);
            if (HashUtility.ComputeFileHash(path) != payloadHash)
                throw new ApplicationException("Invalid payload hash.");

            session.State = State.Uploaded;
        }

        void ExecuteBatFile()
        {
            session.State = State.Executing;

            using (var p = new Process { StartInfo = new ProcessStartInfo() })
            {
                p.StartInfo.FileName = this.settings.ExecutableFile;
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
