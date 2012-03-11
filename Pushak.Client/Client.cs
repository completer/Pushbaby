using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Pushak.Shared;

namespace Pushak.Client
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

            string session = this.ObtainSession();


            var encryptor = CryptoUtility.GetEncryptor(settings.SharedSecret, session);
            string encrypted = encryptor.EncryptString("hello mary!!!");

            var encryptor2 = CryptoUtility.GetEncryptor(settings.SharedSecret, session);
            string decrypted = encryptor2.DecryptString(encrypted);
            bool ok = decrypted == "hello mary";

            
            PostPayload(session);
            GetProgressUntilDone(session);
        }

        string ObtainSession()
        {
            return new WebClient().UploadString(destination, "hello");
        }

        void PostPayload(string session)
        {
            Console.WriteLine("Pushak.Client:: Uploading payload...");

            var request = WebRequest.Create(destination);
            request.Method = "POST";
            request.Headers.Add("session", session);

            var encryptor = CryptoUtility.GetEncryptor(this.settings.SharedSecret, session);


            string payloadHash = HashUtility.ComputeFileHash(payload);
            request.Headers.Add("payload-hash", payloadHash); // todo encrypt

            string filename = Path.GetFileName(payload);
            string filenameHash = HashUtility.ComputeStringHash(filename);
            request.Headers.Add("filename-hash", filenameHash); // todo encrypt

            using (var input = File.OpenRead(payload))
            using (var output = new CryptoStream(request.GetRequestStream(), encryptor, CryptoStreamMode.Write))
            {
                var buffer = new byte[1024];
                int bytesRead;
                do
                {
                    bytesRead = input.Read(buffer, 0, 1024);
                    output.Write(buffer, 0, bytesRead);
                }
                while (bytesRead > 0);
            }

            using (request.GetResponse()) { }

            Console.WriteLine("Pushak.Client:: Uploaded payload.");
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
                    Console.WriteLine(String.IsNullOrEmpty(content) ? "Pushak.Client:: ..." : content);
                }

                Thread.Sleep(this.settings.PollIntervalInSeconds * 1000);
            }
        }
    }
}
