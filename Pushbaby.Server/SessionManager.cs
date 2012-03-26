using System;
using System.Collections.Concurrent;
using System.Linq;
using Pushbaby.Shared;

namespace Pushbaby.Server
{
    public interface ISessionManager
    {
        Session Get(IContext context);
        void Put(Session session);
        void Remove(Session session);
    }

    public class SessionManager : ISessionManager
    {
        readonly ConcurrentDictionary<string, Session> sessions = new ConcurrentDictionary<string, Session>();

        public Session Get(IContext context)
        {
            this.DeleteOldSessions();

            string key = context.RequestHeaders["session"];

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
            if (session.State == State.Executed)
                this.Remove(session);
            else
                this.sessions.TryAdd(session.Key, session);
        }

        void DeleteOldSessions()
        {
            foreach (var session in sessions.Values
                .Where(s => s.CreatedOnUtc < DateTime.UtcNow.AddHours(-2))
                .ToList())
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
