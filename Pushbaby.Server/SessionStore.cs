using System;
using System.Collections.Concurrent;
using System.Linq;
using Pushbaby.Shared;

namespace Pushbaby.Server
{
    public interface ISessionStore
    {
        ISession Get(IContext context);
        void Put(ISession session);
        void Remove(ISession session);
    }

    public class SessionStore : ISessionStore
    {
        readonly ConcurrentDictionary<string, ISession> sessions = new ConcurrentDictionary<string, ISession>();

        public ISession Get(IContext context)
        {
            this.DeleteOldSessions();

            string key = context.RequestHeaders["session"];

            if (key == null)
            {
                return new Session(CryptoUtility.GenerateSessionKey(), DateTime.UtcNow);
            }
            else
            {
                ISession session;
                
                if (this.sessions.TryRemove(key, out session))
                    return session;
                else
                    throw new ApplicationException("Session '" + key + "'does not exist, is in use, or has ended.");
            }
        }

        public void Put(ISession session)
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

        public void Remove(ISession session)
        {
            this.sessions.TryRemove(session.Key, out session);
        }
    }
}
