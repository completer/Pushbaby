using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Pushak.Server
{
    public class Session
    {
        readonly SessionFactory sessionFactory;
        readonly ConcurrentQueue<string> buffer = new ConcurrentQueue<string>();

        public string Key { get; private set; }
        public DateTime CreatedOnUtc { get; private set; }
        public State State { get; set; }

        public Session(string key, DateTime createdOnUtc, SessionFactory sessionFactory)
        {
            this.Key = key;
            this.CreatedOnUtc = createdOnUtc;
            this.sessionFactory = sessionFactory;
        }

        public void Write(string s)
        {
            this.buffer.Enqueue("Pushak.Server:: " + s);
        }

        public string Read()
        {
            var ss = new List<string>();
            string s;
            while (buffer.TryDequeue(out s)) { ss.Add(s); }
            return String.Join(Environment.NewLine, ss);
        }

        public void Remove()
        {
            this.sessionFactory.Remove(this);
        }
    }
}
