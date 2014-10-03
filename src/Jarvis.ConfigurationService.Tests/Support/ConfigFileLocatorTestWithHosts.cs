using Jarvis.ConfigurationService.Host.Support;
using Microsoft.Owin.Hosting;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Jarvis.ConfigurationService.Tests.Support
{
    [TestFixture]
    [Category("HostOn")]
    public class ConfigFileLocatorTestWithHosts
    {
        IDisposable _app;
        WebClient client;
        String baseUri = "http://localhost:53642";
        [TestFixtureSetUp]
        public void FixtureSetup() 
        {
            _app = WebApp.Start<Startup>(baseUri);
            client = new WebClient();
        }

        [TestFixtureTearDown]
        public void FixtureTearDown()
        {
            _app.Dispose();
        }

        [Test]
        public void Smoke_test_root()
        {
            var result = client.DownloadString(baseUri);
            Assert.That(result, Is.Not.Empty);
        }

        [Test]
        public void correct_listing_of_all_applications()
        {
            var result = client.DownloadString(baseUri);
            Assert.That(result, Is.StringContaining(@"Applications"":[""MyApp1"",""MyApp2""]"));
        }

        [Test]
        public void correct_listing_of_all_services_in_application()
        {
            var result = client.DownloadString(baseUri + "/MyApp1");
            Assert.That(result, Is.StringContaining(@"[""Service1"",""Service2""]"));
        }

        [Test]
        public void correct_configuration_of_single_service()
        {
            var result = client.DownloadString(baseUri + "/MyApp1/Service2/config.json");
            Assert.That(result, Is.EqualTo(@"{""connectionStrings"":{""bl"":""mongodb://localhost/bl"",""log"":""mongodb://localhost/log-service1""},""message"":""hello from service 2"",""workers"":""1"",""enableApi"":""false""}"));
        }
    }
}
