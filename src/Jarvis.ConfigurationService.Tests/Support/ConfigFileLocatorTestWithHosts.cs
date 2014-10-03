using Jarvis.ConfigurationService.Host.Support;
using Microsoft.Owin.Hosting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
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

        [Test]
        public void redirect_of_folder_is_working_app_and_service()
        {
            String anotherTestconfigurationDir = Path.Combine(Environment.CurrentDirectory, "AnotherTestConfiguration\\ApplicationX");
            Assert.That(Directory.Exists(anotherTestconfigurationDir), "Test data does not exists");
            String redirectFile = Path.Combine(Environment.CurrentDirectory, "Configuration.Sample\\ApplicationX.redirect");
            File.WriteAllText(redirectFile, anotherTestconfigurationDir);
            var result = client.DownloadString(baseUri + "/ApplicationX/ServiceY/config.json");
            JObject setting = (JObject)JsonConvert.DeserializeObject(result);
            Assert.That((String)setting["ServiceName"], Is.EqualTo("Y")); //specific configuration

            Assert.That((String)setting["workers"], Is.EqualTo("42")); //applicationX configuration   
        }

        [Test]
        public void redirect_of_folder_is_using_same_base_service_for_redirection()
        {
            String anotherTestconfigurationDir = Path.Combine(Environment.CurrentDirectory, "AnotherTestConfiguration\\ApplicationX");
            Assert.That(Directory.Exists(anotherTestconfigurationDir), "Test data does not exists");
            String redirectFile = Path.Combine(Environment.CurrentDirectory, "Configuration.Sample\\ApplicationX.redirect");
            File.WriteAllText(redirectFile, anotherTestconfigurationDir);
            var result = client.DownloadString(baseUri + "/ApplicationX/ServiceY/config.json");
            JObject setting = (JObject)JsonConvert.DeserializeObject(result);

            Assert.That(
                    (String)setting["message"],
                    Is.EqualTo("hello from Configuration Server"),
                    "Even with redirectrion, base config.json is the one located in the root of the configuration server"
                );  
        }
    }
}
