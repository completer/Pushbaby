using System;
using System.Collections.Specialized;
using System.IO;
using System.Net;

namespace Pushbaby.Server
{
    public interface IListener
    {
        void AddPrefix(string uriPrefix);
        void Start();
        IContext GetContext();
    }

    public class Listener : IListener
    {
        readonly HttpListener listener = new HttpListener();

        public void AddPrefix(string uriPrefix)
        {
            this.listener.Prefixes.Add(uriPrefix);
        }

        public void Start()
        {
            this.listener.Start();
        }

        public IContext GetContext()
        {
            return new Context(this.listener.GetContext());
        }
    }

    public interface IContext
    {
        NameValueCollection RequestHeaders { get; }
        string RequestMethod { get; }
        Uri RequestUrl { get; }
        Stream RequestStream { get; }

        NameValueCollection ResponseHeaders { get; }
        Stream ResponseStream { get; }
        int ResponseCode { set; }
        long ResponseLength { set; }
    }

    public class Context : IContext
    {
        readonly HttpListenerContext context;

        public Context(HttpListenerContext context)
        {
            this.context = context;
        }

        public NameValueCollection RequestHeaders
        {
            get { return this.context.Request.Headers;  }
        }

        public string RequestMethod
        {
            get { return this.context.Request.HttpMethod; }
        }

        public Uri RequestUrl
        {
            get { return this.context.Request.Url; }
        }

        public Stream RequestStream
        {
            get { return this.context.Request.InputStream; }
        }

        public NameValueCollection ResponseHeaders
        {
            get { return this.context.Response.Headers; }
        }

        public Stream ResponseStream
        {
            get { return this.context.Response.OutputStream; }
        }

        public int ResponseCode
        {
            set { this.context.Response.StatusCode = value; }
        }

        public long ResponseLength
        {
            set { this.context.Response.ContentLength64 = value; }
        }
    }
}
