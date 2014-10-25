using System.Collections;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using Jarvis.ConfigurationService.Client.Support;
using NSubstitute;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using Jarvis.ConfigurationService.Client;
using Jarvis.ConfigurationService.Host.Support;
using Microsoft.Owin.Hosting;

namespace Jarvis.ConfigurationService.Tests.Client
{
    [TestFixture]
    public class ConfigurationServiceClientWithHostTests
    {
        IDisposable _app;
        WebClient client;
        String baseUri = "http://localhost:53643";

        [TestFixtureSetUp]
        public void FixtureSetup()
        {
            _app = WebApp.Start<ConfigurationServiceApplication>(baseUri);
            client = new WebClient();
        }

        [TestFixtureTearDown]
        public void FixtureTearDown()
        {
            _app.Dispose();
        }

      
    }
}
