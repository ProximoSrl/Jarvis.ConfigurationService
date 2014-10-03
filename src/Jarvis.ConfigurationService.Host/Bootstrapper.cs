using System;
using Jarvis.ConfigurationService.Host.Support;
using log4net;
using Microsoft.Owin.Hosting;

namespace Jarvis.ConfigurationService.Host
{
    internal class Bootstrapper
    {
        readonly Uri _uri;
        IDisposable _app;
        public Bootstrapper(Uri uri)
        {
            _uri = uri;
        }

        public void Start()
        {
            LogManager.GetLogger(this.GetType()).DebugFormat("Starting on {0}", _uri.AbsoluteUri);
            _app = WebApp.Start<Startup>(_uri.AbsoluteUri);
            
        }

        public void Stop()
        {
            _app.Dispose();
        }
    }
}