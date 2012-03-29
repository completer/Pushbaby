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
using System.Threading.Tasks;
using Ionic.Zip;
using Pushbaby.Shared;

namespace Pushbaby.Client
{
    public class Client
    {
        static void Main(string[] args)
        {
            string payload  = args.FirstOrDefault();
            var destinations = args.Skip(1).ToList();

            if (payload == null)
                throw new ArgumentException("You must specify a payload file or directory path.");

            if (!destinations.Any())
                throw new ArgumentException("You must specify at least one destination URL.");

            new Client(new Settings(), payload, destinations).Run();
        }

        readonly Settings settings;
        readonly string payload;
        readonly List<string> destinations;

        public Client(Settings settings, string payload, List<string> destinations)
        {
            this.settings = settings;
            this.payload = payload;
            this.destinations = destinations;
        }

        public void Run()
        {
            WaitForServerToStartIfDebugging();
            this.settings.Validate();
            ServicePointManager.Expect100Continue = false;

            Parallel.ForEach(this.destinations, destination =>
                {
                    string session = this.ObtainSession(destination);
                    PostPayload(destination, session);
                    GetProgressUntilDone(destination, session);
                });
        }

        string ObtainSession(string destination)
        {
            return new WebClient().UploadString(destination, "hello");
        }

        void PostPayload(string destination, string session)
        {
            Console.WriteLine(destination + ":: Uploading payload...");

            using (var payloadInfo = this.PreparePayload())
            {
                var request = WebRequest.Create(destination);
                request.Method = "POST";
                request.Headers.Add("session", session);

                var aes = CryptoUtility.GetAlgorithm(settings.SharedSecret, session);

                // send the filename and its hash
                request.Headers.Add("filename", aes.EncryptString(payloadInfo.Name));
                string filenameHash = HashUtility.ComputeStringHash(payloadInfo.Name);
                request.Headers.Add("filename-hash", aes.EncryptString(filenameHash));

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

            Console.WriteLine(destination + ":: Uploaded payload.");
        }

        void GetProgressUntilDone(string destination, string session)
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

                    while (!reader.EndOfStream)
                    {
                        string line = reader.ReadLine();
                        Console.WriteLine(destination + ":: " + line);
                    }
                }

                Thread.Sleep(this.settings.PollIntervalInSeconds * 1000);
            }
        }

        [Conditional("DEBUG")]
        static void WaitForServerToStartIfDebugging()
        {
            Thread.Sleep(2000);
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
                        Disposer = () => File.Delete(path)
                    };
            }
            else
            {
                throw new ApplicationException(String.Format("File or directory '{0}' does not exist.", payload));
            }
        }
    }
}
