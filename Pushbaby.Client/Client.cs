using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
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
            string payload = args.FirstOrDefault(a => !a.StartsWith("/"));
            var destinations = args.Where(a => !a.StartsWith("/")).Skip(1).ToList();

            string tag = (from a in args
                          let match = Regex.Match(a, @"\/tag:(.+)")
                          where match.Success
                          select match.Groups[1].Value).SingleOrDefault();

            bool verbose = (from a in args
                            where a.StartsWith("/verbose")
                            select true).SingleOrDefault();

            if (payload == null)
                throw new ArgumentException("You must specify a payload file or directory path.");

            if (!destinations.Any())
                throw new ArgumentException("You must specify at least one destination URL.");

            new Client(new Settings(), payload, destinations, tag, verbose).Run();
        }

        readonly Settings settings;
        readonly string payload;
        readonly string tag;
        readonly List<string> destinations;
        readonly bool verbose;

        public Client(Settings settings, string payload, List<string> destinations, string tag, bool verbose)
        {
            this.settings = settings;
            this.payload = payload;
            this.tag = tag;
            this.destinations = destinations;
            this.verbose = verbose;
        }

        public void Run()
        {
            WaitForServerToStartIfDebugging();
            this.settings.Validate();
            ServicePointManager.Expect100Continue = false;

            using (var payloadInfo = this.PreparePayload())
            {
                Parallel.ForEach(this.destinations, destination =>
                    {
                        string session = this.ObtainSession(destination);
                        PostPayload(payloadInfo, destination, session);
                        GetProgressUntilDone(destination, session);
                    });
            }
        }

        string ObtainSession(string destination)
        {
            Console.WriteLine(destination + ":: Obtaining session...");
            return new WebClient().UploadString(destination, "hello");
        }

        void PostPayload(PayloadInfo payloadInfo, string destination, string session)
        {
            Console.WriteLine(destination + ":: Uploading payload...");

            var request = WebRequest.Create(destination);
            request.Method = "POST";
            request.Headers.Add("session", session);
            request.Timeout = 500000; // 500 seconds

            var aes = CryptoUtility.GetAlgorithm(settings.SharedSecret, session);

            // send the tag
            string tagOrDefault = !String.IsNullOrWhiteSpace(tag) ? tag : payloadInfo.Name;
            request.Headers.Add("tag", aes.EncryptString(tagOrDefault));
            request.Headers.Add("tag-hash", aes.EncryptString(HashUtility.ComputeStringHash(tagOrDefault)));

            // send the payload
            request.Headers.Add("payload-hash", aes.EncryptString(HashUtility.ComputeFileHash(payloadInfo.Path)));
            using (var input = File.OpenRead(payloadInfo.Path))
            using (var output = new CryptoStream(request.GetRequestStream(), aes.CreateEncryptor(), CryptoStreamMode.Write))
            {
                StreamUtility.Copy(input, output);
            }

            using (request.GetResponse()) { }                

            Console.WriteLine(destination + ":: Uploaded payload.");
        }

        void GetProgressUntilDone(string destination, string session)
        {
            string state = null;

            while (state != "Executed" && state != "Failed")
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

                    if (state == "Failed")
                        throw new ApplicationException(destination + ":: Execution of deployment script failed.");
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
            Console.WriteLine("Preparing payload...");

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
                string path = Path.GetTempFileName();
                
                using (var zip = new ZipFile())
                {
                    if (this.verbose)
                    {
                        zip.SaveProgress += (sender, e) =>
                        {
                            Console.WriteLine(string.Format("Event: {0}. Items Saved: {1} of {2}", e.EventType, e.EntriesSaved, e.EntriesTotal));
                            if (null != e.CurrentEntry)
                                Console.WriteLine(e.CurrentEntry.FileName);
                        };
                    }

                    zip.AddDirectory(payload);
                    zip.Save(path);
                }

                return new PayloadInfo
                    {
                        Name = Path.GetFileName(payload),
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
