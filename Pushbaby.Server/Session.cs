using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Pushbaby.Server
{
    public interface ISession
    {
        string Key { get; }
        DateTime CreatedOnUtc { get; }
        State State { get; set; }
        
        void WriteProgress(string s);
        string ReadProgress();
    }

    public class Session : ISession
    {
        readonly ConcurrentQueue<string> progress = new ConcurrentQueue<string>();

        public string Key { get; private set; }
        public DateTime CreatedOnUtc { get; private set; }
        public State State { get; set; }

        public Session(string key, DateTime createdOnUtc)
        {
            this.Key = key;
            this.CreatedOnUtc = createdOnUtc;
        }

        public void WriteProgress(string s)
        {
            this.progress.Enqueue(s);
        }

        public string ReadProgress()
        {
            var ss = new List<string>();
            string s;
            while (this.progress.TryDequeue(out s)) { ss.Add(s); }
            return String.Join(Environment.NewLine, ss);
        }
    }
}
