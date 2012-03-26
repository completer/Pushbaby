using System;
using System.Threading;

namespace Pushbaby.Server
{
    public interface IThreadManager
    {
        void Queue(Action work);
        void Create(Action work);
    }

    public class ThreadManager : IThreadManager
    {
        public void Queue(Action work)
        {
            ThreadPool.QueueUserWorkItem(x => work());
        }

        public void Create(Action work)
        {
            new Thread(x => work()) { IsBackground = false }.Start();
        }
    }
}
