using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Ionic.Zip;
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
            this.ValidatePayload();
            ThreadPool.QueueUserWorkItem(x => this.ExecuteBatFile());
            this.WriteResponse("OK");
        }

        public void HandleProgress()
        {
            this.log.InfoFormat("Handling progress for session {0}...", session.Key);

            this.WriteResponse(session.ReadProgress());
        }

        public void HandleError()
        {
            this.WriteResponse("Pushbaby.Server:: Unhandled exception. See server log.");
        }

        void SavePayloadToDisk()
        {
            session.State = State.Uploading;

            // todo: let's split method this out a bit

            var aes = CryptoUtility.GetAlgorithm(settings.SharedSecret, this.session.Key);

            string filename = aes.DecryptString(context.Request.Headers["filename"] ?? String.Empty);
            string filenameHash = aes.DecryptString(context.Request.Headers["filename-hash"] ?? String.Empty);

            if (HashUtility.ComputeStringHash(filename) != filenameHash)
                throw new ApplicationException("Filename hash did not match.");

            if (String.IsNullOrWhiteSpace(filename))
                throw new ApplicationException("Empty filename was given.");

            string path = Path.Combine(this.settings.DeploymentDirectory, filename);

            using (var output = File.Create(path))
            using (var input = new CryptoStream(context.Request.InputStream, aes.CreateDecryptor(), CryptoStreamMode.Read))
            {
                StreamUtility.Copy(input, output);
            }

            string payloadHash = aes.DecryptString(context.Request.Headers["payload-hash"] ?? String.Empty);

            if (HashUtility.ComputeFileHash(path) != payloadHash)
                throw new ApplicationException("Payload hash did not match.");

            if (new FileInfo(path).Length < 16)
                throw new ApplicationException("File was too small.");

            string extension = Path.GetExtension(filename);
            if (extension != null && extension.ToLowerInvariant() == ".zip")
            {
                using (var zip = new ZipFile(path))
                {
                    string unzippedPath = Path.Combine(this.settings.DeploymentDirectory, Path.GetFileNameWithoutExtension(filename));
                    zip.ExtractAll(unzippedPath, ExtractExistingFileAction.OverwriteSilently);
                }
                File.Delete(path);
            }

            session.State = State.Uploaded;
        }

        void ValidatePayload()
        {
            // ensure the name and content of the payload are non-zero
        }

        void ExecuteBatFile()
        {
            session.State = State.Executing;

            using (var p = new Process { StartInfo = new ProcessStartInfo() })
            {
                p.StartInfo.FileName = this.settings.ExecutableFile;
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardOutput = true;
                p.OutputDataReceived += (s, e) => session.WriteProgress(e.Data);

                session.WriteProgress("Running bat file...");
                p.Start();
                p.BeginOutputReadLine();
                p.WaitForExit();

                if (p.ExitCode > 0)
                {
                    session.WriteProgress("Exited with code " + p.ExitCode);
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

        void Unzip()
        {
        }

    }
}
