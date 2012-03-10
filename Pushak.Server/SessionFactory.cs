using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using Pushak.Shared;

namespace Pushak.Server
{
    public class SessionFactory
    {
        readonly ConcurrentDictionary<string, Session> sessions = new ConcurrentDictionary<string, Session>();

        public Session Get(HttpListenerContext context)
        {
            this.DeleteOldSessions();

            string key = context.Request.Headers["session"];

            if (key == null)
            {
                var session = new Session(CryptoUtility.GenerateKey(), DateTime.UtcNow);
                this.sessions.TryAdd(session.Key, session);
                return session;
            }
            else
            {
                Session session;
                if (this.sessions.TryGetValue(key, out session))
                    return session;
                else
                    throw new InvalidOperationException("Session does not exist or has ended.");
            }
        }

        void DeleteOldSessions()
        {
            foreach (var session in sessions.Values.Where(s => s.CreatedOnUtc < DateTime.UtcNow.AddHours(-2)))
            {
                this.Remove(session);
            }
        }

        public void Remove(Session session)
        {
            this.sessions.TryRemove(session.Key, out session);
        }
    }
}
