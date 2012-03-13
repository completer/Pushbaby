using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using Pushbaby.Shared;
using log4net;

namespace Pushbaby.Server
{
    public class SessionManager
    {
        readonly ILog log;
        readonly ConcurrentDictionary<string, Session> sessions = new ConcurrentDictionary<string, Session>();

        public SessionManager(ILog log)
        {
            this.log = log;
        }

        public Session Get(HttpListenerContext context)
        {
            this.DeleteOldSessions();

            string key = context.Request.Headers["session"];

            if (key == null)
            {
                return new Session(CryptoUtility.GenerateSessionKey(), DateTime.UtcNow);
            }
            else
            {
                Session session;
                
                if (this.sessions.TryRemove(key, out session))
                    return session;
                else
                    throw new ApplicationException("Session '" + key + "'does not exist, is in use, or has ended.");
            }
        }

        public void Put(Session session)
        {
            this.sessions.TryAdd(session.Key, session);
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
