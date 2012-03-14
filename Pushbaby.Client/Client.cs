using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Ionic.Zip;
using Pushbaby.Shared;

namespace Pushbaby.Client
{
    public class Client
    {
        static void Main(string[] args)
        {
            string payload  = args.ElementAt(0);
            string destination = args.ElementAtOrDefault(1);

            if (payload == null)
                throw new ArgumentException("You must specify a payload file or directory path.");

            if (destination == null)
                throw new ArgumentException("You must specify a destination URL.");

            new Client(new Settings(), payload, destination).Run();
        }

        readonly Settings settings;
        readonly string payload;
        readonly string destination;

        public Client(Settings settings, string payload, string destination)
        {
            this.settings = settings;
            this.payload = payload;
            this.destination = destination;
        }

        public void Run()
        {
            WaitForServerToStart();
            this.settings.Validate();
            ServicePointManager.Expect100Continue = false;

            string session = this.ObtainSession();
            PostPayload(session);
            GetProgressUntilDone(session);
        }

        string ObtainSession()
        {
            return new WebClient().UploadString(destination, "hello");
        }

        void PostPayload(string session)
        {
            Console.WriteLine("Pushbaby.Client:: Uploading payload...");

            var payloadInfo = this.PreparePayload();

            try
            {
                var request = WebRequest.Create(destination);
                request.Method = "POST";
                request.Headers.Add("session", session);

                var aes = CryptoUtility.GetAlgorithm(settings.SharedSecret, session);

                // send the filename and its hash
                request.Headers.Add("filename", aes.EncryptString(payloadInfo.Name));
                string payloadNameHash = HashUtility.ComputeStringHash(payloadInfo.Name);
                request.Headers.Add("filename-hash", aes.EncryptString(payloadNameHash));

                // send the payload hash
                string payloadHash = HashUtility.ComputeFileHash(payloadInfo.Path);
                request.Headers.Add("payload-hash", aes.EncryptString(payloadHash));

                // send the payload
                using (var input = File.OpenRead(payloadInfo.Path))
                using (var output = new CryptoStream(request.GetRequestStream(), aes.CreateEncryptor(), CryptoStreamMode.Write))
                {
                    StreamUtility.Copy(input, output);
                }

                using (request.GetResponse()) { }
            }
            finally
            {
                // clean up the payload file
                if (payloadInfo.Delete)
                    File.Delete(payloadInfo.Path);
            }

            Console.WriteLine("Pushbaby.Client:: Uploaded payload.");
        }

        void GetProgressUntilDone(string session)
        {
            string state = null;

            while (state != "Executed")
            {
                var request = WebRequest.Create(destination);
                request.Headers.Add("session", session);

                using (var response = request.GetResponse())
                using (var reader = new StreamReader(response.GetResponseStream()))
                {
                    // todo: ensure that progress reports are sequential
                    state = response.Headers["state"];
                    string content = reader.ReadToEnd();
                    Console.WriteLine(String.IsNullOrEmpty(content) ? "Pushbaby.Client:: ..." : content);
                }

                Thread.Sleep(this.settings.PollIntervalInSeconds * 1000);
            }
        }

        [Conditional("DEBUG")]
        static void WaitForServerToStart()
        {
            Thread.Sleep(1000);
        } 

        /// <summary>
        /// Gets the payload info, zipping a directory if necessary.
        /// </summary>
        PayloadInfo PreparePayload()
        {
            if (File.Exists(payload))
            {
                return new PayloadInfo
                    {
                        Name = Path.GetFileName(payload),
                        Path = payload,
                        Delete = false,
                    };
            }
            else if (Directory.Exists(payload))
            {
                string path = Path.Combine(Path.GetTempPath(), Path.GetTempFileName());
                
                using (var zip = new ZipFile())
                {
                    zip.AddDirectory(payload);
                    zip.Save(path);
                }

                return new PayloadInfo
                    {
                        Name = Path.GetFileName(payload) + ".zip",
                        Path = path,
                        Delete = true,
                    };
            }
            else
            {
                throw new ApplicationException(String.Format("File or directory '{0}' does not exist.", payload));
            }
        }
    }
}
