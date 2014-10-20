using System;
using Jarvis.ConfigurationService.Host.Support;
using log4net;
using Microsoft.Owin.Hosting;

namespace Jarvis.ConfigurationService.Host
{
    internal class ConfigurationServiceBootstrapper
    {
        readonly Uri _uri;
        IDisposable _app;
        public ConfigurationServiceBootstrapper(Uri uri)
        {
            _uri = uri;
        }

        public void Start()
        {
            LogManager.GetLogger(this.GetType()).DebugFormat("Starting on {0}", _uri.AbsoluteUri);
            _app = WebApp.Start<ConfigurationServiceApplication>(_uri.AbsoluteUri);
            
        }

        public void Stop()
        {
            _app.Dispose();
        }
    }
}