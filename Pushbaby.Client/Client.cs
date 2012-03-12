using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Pushbaby.Shared;

namespace Pushbaby.Client
{
    public class Client
    {
        static void Main(string[] args)
        {
            Thread.Sleep(100);

            string payload  = args.ElementAt(0);
            string destination = args.ElementAtOrDefault(1) ?? "http://localhost/pushak/";

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

            var request = WebRequest.Create(destination);
            request.Method = "POST";
            request.Headers.Add("session", session);

            var aes = CryptoUtility.GetAlgorithm(settings.SharedSecret, session);

            // send the filename
            string filename = Path.GetFileName(payload);
            request.Headers.Add("filename", aes.EncryptString(filename));

            // send the filename hash
            string filenameHash = HashUtility.ComputeStringHash(filename);
            request.Headers.Add("filename-hash", aes.EncryptString(filenameHash));

            // send the payload hash
            string payloadHash = HashUtility.ComputeFileHash(payload);
            request.Headers.Add("payload-hash", aes.EncryptString(payloadHash));

            // send the payload
            using (var input = File.OpenRead(payload))
            using (var output = new CryptoStream(request.GetRequestStream(), aes.CreateEncryptor(), CryptoStreamMode.Write))
            {
                StreamUtility.Copy(input, output);
            }

            using (request.GetResponse()) { }

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
    }
}
