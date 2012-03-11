using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using Pushak.Shared;
using log4net;

namespace Pushak.Server
{
    public class SessionFactory
    {
        readonly ILog log;
        readonly ConcurrentDictionary<string, Session> sessions = new ConcurrentDictionary<string, Session>();

        public SessionFactory(ILog log)
        {
            this.log = log;
        }

        public Session Get(HttpListenerContext context)
        {
            this.DeleteOldSessions();

            string key = context.Request.Headers["session"];

            if (key == null)
            {
                var session = new Session(CryptoUtility.GenerateSessionKey(), DateTime.UtcNow, this);
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
