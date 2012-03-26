using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Ionic.Zip;
using Pushbaby.Shared;
using log4net;

namespace Pushbaby.Server
{
    public class Handler
    {
        readonly ILog log;
        readonly EndpointSettings settings;
        readonly IContext context;
        readonly ISessionStore sessionStore;
        readonly IThreadManager threadManager;

        public Handler(ILog log, EndpointSettings settings, IContext context, ISessionStore sessionStore, IThreadManager threadManager)
        {
            this.log = log;
            this.settings = settings;
            this.context = context;
            this.sessionStore = sessionStore;
            this.threadManager = threadManager;
        }

        public void Handle()
        {
            try
            {
                var session = this.sessionStore.Get(this.context);

                if (this.context.RequestHeaders["session"] == null)
                {
                    if (this.context.RequestMethod == "POST")
                        this.HandleGreeting(session);
                    else
                        this.HandleHomepage(session);
                }
                else
                {
                    if (this.context.RequestMethod == "POST")
                        this.HandlePayload(session);
                    else
                        this.HandleProgress(session);
                }

                this.sessionStore.Put(session);
            }
            catch (Exception ex)
            {
                this.log.ErrorFormat("Unhandled exception. Request: {0}. Exception: {1}", context.RequestUrl, ex);
                this.context.ResponseCode = 500;

                using (var writer = new StreamWriter(this.context.ResponseStream))
                {
                    writer.Write("Pushbaby.Server:: Unhandled exception. See server log.");
                }
            }
        }

        public void HandleHomepage(ISession session)
        {
            this.log.Info("Handling homepage...");

            this.WriteResponse(session, "Pushbaby.Server:: Server is running. " + DateTime.UtcNow.ToLongDateString());
        }

        public void HandleGreeting(ISession session)
        {
            this.log.InfoFormat("Handling greeting for session {0}...", session.Key);

            this.WriteResponse(session, session.Key);
            session.State = State.Greeted;
        }

        public void HandlePayload(ISession session)
        {
            this.log.InfoFormat("Handling payload for session {0}...", session.Key);

            string payloadPath = this.SavePayloadToDisk(session);
            this.threadManager.Queue(() => this.ExecuteBatFile(session, payloadPath));
            this.WriteResponse(session, "OK");
        }

        public void HandleProgress(ISession session)
        {
            this.log.InfoFormat("Handling progress for session {0}...", session.Key);

            this.WriteResponse(session, session.ReadProgress());
        }

        public void HandleError(ISession session)
        {
            this.WriteResponse(session, "Pushbaby.Server:: Unhandled exception. See server log.");
        }

        string SavePayloadToDisk(ISession session)
        {
            session.State = State.Uploading;

            // todo: let's split method this out a bit

            var aes = CryptoUtility.GetAlgorithm(settings.SharedSecret, session.Key);

            string filename = aes.DecryptString(context.RequestHeaders["filename"] ?? String.Empty);
            string filenameHash = aes.DecryptString(context.RequestHeaders["filename-hash"] ?? String.Empty);

            if (HashUtility.ComputeStringHash(filename) != filenameHash)
                throw new ApplicationException("Filename hash did not match.");

            if (String.IsNullOrWhiteSpace(filename))
                throw new ApplicationException("Empty filename was given.");

            string path = Path.Combine(this.settings.PayloadDirectory, filename);

            using (var output = File.Create(path))
            using (var input = new CryptoStream(context.RequestStream, aes.CreateDecryptor(), CryptoStreamMode.Read))
            {
                StreamUtility.Copy(input, output);
            }

            string payloadHash = aes.DecryptString(context.RequestHeaders["payload-hash"] ?? String.Empty);

            if (HashUtility.ComputeFileHash(path) != payloadHash)
                throw new ApplicationException("Payload hash did not match.");

            if (new FileInfo(path).Length < 16)
                throw new ApplicationException("Payload was too small.");

            string finalPayloadPath = path;
            string extension = Path.GetExtension(filename);
            if (extension != null && extension.ToLowerInvariant() == ".zip")
            {
                using (var zip = new ZipFile(path))
                {
                    finalPayloadPath = Path.Combine(this.settings.PayloadDirectory, Path.GetFileNameWithoutExtension(filename));
                    zip.ExtractAll(finalPayloadPath, ExtractExistingFileAction.OverwriteSilently);
                }
                File.Delete(path);
            }

            session.State = State.Uploaded;

            return finalPayloadPath;
        }

        void ExecuteBatFile(ISession session, string payloadPath)
        {
            session.State = State.Executing;

            using (var p = new Process { StartInfo = new ProcessStartInfo() })
            {
                p.StartInfo.FileName = this.settings.ExecutableFile;
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.Arguments = payloadPath.Enquote();
                p.OutputDataReceived += (s, e) => session.WriteProgress(e.Data);

                session.WriteProgress("Running executable file...");
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

        void WriteResponse(ISession session, string s)
        {
            this.context.ResponseHeaders.Add("state", session.State.ToString());

            var bytes = Encoding.UTF8.GetBytes(s);

            using (var output = this.context.ResponseStream)
            {
                this.context.ResponseLength = bytes.Length;
                output.Write(bytes, 0, bytes.Length);
            }
        }
    }
}
