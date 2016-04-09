using System;
using log4net;
using Microsoft.Owin.Hosting;

namespace Jarvis.ConfigurationService.Host.Support
{
    internal class ConfigurationServiceBootstrapper
    {
        readonly Uri _uri;
        IDisposable _app;
        private ILog _logger;
        public ConfigurationServiceBootstrapper(Uri uri)
        {
            _uri = uri;
        }

        public void Start()
        {
            try
            {
                _logger = LogManager.GetLogger(this.GetType());
                _logger.DebugFormat("Starting on {0}", _uri.AbsoluteUri);
                _app = WebApp.Start<ConfigurationServiceApplication>(_uri.AbsoluteUri);
            }
            catch (Exception ex)
            {
                _logger.Error("Exception: " + ex.Message, ex); 
                throw;
            }
         
        }

        public void Stop()
        {
            _app.Dispose();
        }
    }
}