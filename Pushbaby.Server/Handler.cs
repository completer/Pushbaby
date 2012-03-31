using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Ionic.Zip;
using Pushbaby.Shared;
using log4net;

namespace Pushbaby.Server
{
    public class Handler
    {
        readonly ILog log;
        readonly EndpointSettings settings;
        readonly ISessionStore sessionStore;
        readonly IThreadManager threadManager;
        readonly IContext context;
        readonly IFileSystem fileSystem;

        public Handler(ILog log, EndpointSettings settings, ISessionStore sessionStore, IThreadManager threadManager, IContext context, IFileSystem fileSystem)
        {
            this.log = log;
            this.settings = settings;
            this.sessionStore = sessionStore;
            this.threadManager = threadManager;
            this.context = context;
            this.fileSystem = fileSystem;
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
            this.threadManager.Queue(() => this.ExecuteBatFileAndDeleteOldPayloadDirectories(session, payloadPath));
            this.WriteResponse(session, "OK");
        }

        public void HandleProgress(ISession session)
        {
            this.log.InfoFormat("Handling progress for session {0}...", session.Key);

            this.WriteResponse(session, session.ReadProgress());
        }

        string SavePayloadToDisk(ISession session)
        {
            session.State = State.Uploading;

            var aes = CryptoUtility.GetAlgorithm(settings.SharedSecret, session.Key);

            string temp = Path.Combine(this.settings.DeploymentDirectory, this.GetNextPayloadDirectory() + ".zip");
            string path = Path.Combine(this.settings.DeploymentDirectory, this.GetNextPayloadDirectory());

            using (var output = File.Create(temp))
            using (var input = new CryptoStream(context.RequestStream, aes.CreateDecryptor(), CryptoStreamMode.Read))
            {
                StreamUtility.Copy(input, output);
            }

            string hash = aes.DecryptString(context.RequestHeaders["hash"]);

            if (HashUtility.ComputeFileHash(temp) != hash)
                throw new ApplicationException("Payload hash did not match.");

            if (new FileInfo(temp).Length < 16)
                throw new ApplicationException("Payload was too small.");

            using (var zip = new ZipFile(temp))
            {
                zip.ExtractAll(path, ExtractExistingFileAction.OverwriteSilently);
            }

            File.Delete(temp);

            session.State = State.Uploaded;

            return path;
        }

        void ExecuteBatFileAndDeleteOldPayloadDirectories(ISession session, string payloadPath)
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
                    session.State = State.Failed;
                    return;
                }
            }

            session.State = State.Executed;

            // only delete old directories if successful
            this.DeleteOldPayloadDirectories();
        }

        void WriteResponse(ISession session, string s)
        {
            this.context.ResponseHeaders.Add("state", session.State.ToString());

            using (var writer = new StreamWriter(this.context.ResponseStream))
            {
                writer.Write(s);
            }
        }

        string GetNextPayloadDirectory()
        {
            return "pushbaby." + (this.GetPayloadDirectoriesInAscendingOrder().Select(d => d.Item3).LastOrDefault() + 1);
        }

        void DeleteOldPayloadDirectories()
        {
            // leave the last 5 directories there for extra safety
            foreach (var d in this.GetPayloadDirectoriesInAscendingOrder().Reverse().Skip(this.settings.SnakeLength))
            {
                Directory.Delete(d.Item1, true);
            }
        }

        IEnumerable<Tuple<string, string, int>> GetPayloadDirectoriesInAscendingOrder()
        {
            var q = from path in this.fileSystem.GetDirectories(this.settings.DeploymentDirectory, "pushbaby.*")
                    let name = Path.GetFileName(path)
                    let number = GetPayloadDirectoryNumber(name)
                    orderby number
                    select new Tuple<string, string, int>(path, name, number);

            return q;
        }

        static int GetPayloadDirectoryNumber(string name)
        {
            string number = Regex.Match(name, @"pushbaby\.(\d+)", RegexOptions.IgnoreCase).Groups[1].Value;
            return Convert.ToInt32(number);
        }
    }
}
