using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using Pushak.Shared;

namespace Pushak.Client
{
    class Program
    {
        static void Main(string[] args)
        {
            string payload  = args.ElementAt(0);
            string destination = args.ElementAtOrDefault(1) ?? "http://localhost/pushak/";

            string session = PostGreeting(payload, destination);
            PostPayload(payload, destination, session);
            GetProgressUntilDone(destination, session);
        }

        static string PostGreeting(string payload, string destination)
        {
            string hash = HashUtility.ComputeHash(payload);
            return new WebClient().UploadString(destination, hash);
        }

        static void PostPayload(string payload, string destination, string session)
        {
            Console.WriteLine("Pushak.Client:: Uploading payload...");

            var request = WebRequest.Create(destination);
            request.Method = "POST";
            request.Headers.Add("session", session);
            request.Headers.Add("filename", Path.GetFileName(payload));

            using (var fs = File.OpenRead(payload))
            using (var rs = request.GetRequestStream())
            {
                var buffer = new byte[1024];
                int bytesRead;
                do
                {
                    bytesRead = fs.Read(buffer, 0, 1024);
                    rs.Write(buffer, 0, bytesRead);
                }
                while (bytesRead > 0);
            }

            using (request.GetResponse()) { }

            Console.WriteLine("Pushak.Client:: Uploaded payload.");
        }

        static void GetProgressUntilDone(string destination, string session)
        {
            string state = null;
            while (state != "Executed")
            {
                var request = WebRequest.Create(destination);
                request.Headers.Add("session", session);

                using (var response = request.GetResponse())
                using (var reader = new StreamReader(response.GetResponseStream()))
                {
                    state = response.Headers["state"];
                    string content = reader.ReadToEnd();
                    Console.WriteLine(String.IsNullOrEmpty(content) ? "Pushak.Client:: ..." : content);
                }

                Thread.Sleep(1000);
            }
        }
    }
}
