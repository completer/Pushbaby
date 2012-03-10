using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;

namespace Pushak.Client
{
    class Program
    {
        static void Main(string[] args)
        {
            string payload  = args.ElementAt(0);
            string destination = args.ElementAtOrDefault(1) ?? "http://localhost/pushak/";

            PostPayload(payload, destination);
            GetProgressUntilDone(destination);
        }

        static void PostPayload(string payload, string destination)
        {
            Console.WriteLine("Pushak.Client:: Uploading payload...");

            var request = WebRequest.Create(destination);
            request.Method = "POST";

            using (var fileStream = File.OpenRead(payload))
            using (var requestStream = request.GetRequestStream())
            {
                var buffer = new byte[1024];
                int bytesRead;
                do
                {
                    bytesRead = fileStream.Read(buffer, 0, 1024);
                    requestStream.Write(buffer, 0, bytesRead);
                }
                while (bytesRead > 0);
            }

            request.Headers.Add("filename", Path.GetFileName(payload));
            using (request.GetResponse()) { }

            Console.WriteLine("Pushak.Client:: Uploaded payload.");
        }

        static void GetProgressUntilDone(string destination)
        {
            string state = null;
            while (state != "Executed")
            {
                var request = WebRequest.Create(destination);
                using (var response = request.GetResponse())
                using (var reader = new StreamReader(response.GetResponseStream()))
                {
                    state = response.Headers["state"];
                    string content = reader.ReadToEnd();
                    if (String.IsNullOrEmpty(content))
                    {
                        Console.WriteLine("Pushak.Client:: ...");
                    }
                    else
                    {
                        Console.WriteLine(content);
                    }
                }

                Thread.Sleep(1000);
            }
        }
    }
}
