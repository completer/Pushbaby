using System;

namespace Pushbaby.Client
{
    public class PayloadInfo : IDisposable
    {
        public string Name { get; set; }
        public string Path { get; set; }

        public Action Disposer { get; set; }

        public void Dispose()
        {
            if (this.Disposer != null)
                this.Disposer();
        }
    }
}
