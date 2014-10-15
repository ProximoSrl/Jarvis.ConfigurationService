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
    public class ConfigControllerTestsWithHost
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
            var redirectFiles = Directory.GetFiles(FileSystem.Instance.GetBaseDirectory(), "*.redirect");
            foreach (var file in redirectFiles)
            {
                File.Delete(file);
            }
            File.WriteAllText(Path.Combine(FileSystem.Instance.GetBaseDirectory(), "myapp3.redirect"), @"c:\temp\myapp3");
            var result = client.DownloadString(baseUri);
            Assert.That(result, Is.StringContaining(@"Applications"":[""MyApp1"",""MyApp2"",""myapp3""]"));
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
            var result = client.DownloadString(baseUri + "/MyApp1/Service2.config");
            Assert.That(result, Is.EqualTo(@"{""connectionStrings"":{""bl"":""mongodb://localhost/bl"",""log"":""mongodb://localhost/log-service1""},""message"":""hello from service 2"",""instruction"":""This is the base configuration file for the entire MyApp1 application"",""workers"":""1"",""enableApi"":""false""}"));
        }

        [Test]
        public void correct_configuration_of_single_service_with_old_route()
        {
            var result = client.DownloadString(baseUri + "/MyApp1/Service2/config.json");
            Assert.That(result, Is.EqualTo(@"{""connectionStrings"":{""bl"":""mongodb://localhost/bl"",""log"":""mongodb://localhost/log-service1""},""message"":""hello from service 2"",""instruction"":""This is the base configuration file for the entire MyApp1 application"",""workers"":""1"",""enableApi"":""false""}"));
        }

        [Test]
        public void override_configuration_for_specific_host()
        {
            var result = client.DownloadString(baseUri + "/MyApp1/Service1.config/Host1");
            JObject jobj = (JObject)JsonConvert.DeserializeObject(result);
            Assert.That((String)jobj["enableApi"], Is.EqualTo("true"), "Override for host failed");
        }


        [Test]
        public void override_configuration_for_specific_host_old_routing()
        {
            var result = client.DownloadString(baseUri + "/MyApp1/Service1/config.json/Host1");
            JObject jobj = (JObject)JsonConvert.DeserializeObject(result);
            Assert.That((String)jobj["enableApi"], Is.EqualTo("true"), "Override for host failed");
        }

        [Test]
        public void not_exsisting_specific_host_should_return_correct_configuration()
        {
            var result = client.DownloadString(baseUri + "/MyApp1/Service1.config/NonExistingHost");
            JObject jobj = (JObject)JsonConvert.DeserializeObject(result);
            Assert.That((String)jobj["enableApi"], Is.EqualTo("false"), "Non existing host should not give error");
        }

        [Test]
        public void specific_host_use_default_configuration()
        {
            var result = client.DownloadString(baseUri + "/MyApp1/Service1.config/Host1");
            JObject jobj = (JObject)JsonConvert.DeserializeObject(result);
            Assert.That((String)jobj["workers"], Is.EqualTo("1"), "If host does not override setting, is should be taken from Default");
        }

        [Test]
        public void verify_encrypted_settings()
        {
            var result = client.DownloadString(baseUri + "/MyApp1/Service1.config/");
            JObject resultObject = (JObject)JsonConvert.DeserializeObject(result);
            Assert.That((String) resultObject["encryptedSetting"], Is.EqualTo("my password"));
        }

        [Test]
        public void redirect_of_folder_is_working_app_and_service()
        {
            String anotherTestconfigurationDir = Path.Combine(Environment.CurrentDirectory, "AnotherTestConfiguration\\ApplicationX");
            Assert.That(Directory.Exists(anotherTestconfigurationDir), "Test data does not exists");
            String redirectFile = Path.Combine(Environment.CurrentDirectory, "Configuration.Sample\\ApplicationX.redirect");
            File.WriteAllText(redirectFile, anotherTestconfigurationDir);
            var result = client.DownloadString(baseUri + "/ApplicationX/ServiceY.config");
            JObject setting = (JObject)JsonConvert.DeserializeObject(result);
            Assert.That((String)setting["ServiceName"], Is.EqualTo("Y")); //specific configuration

            Assert.That((String)setting["workers"], Is.EqualTo("42")); //applicationX configuration   
        }

        [Test]
        public void redirect_of_folder_is_working_when_listing_services()
        {
            String anotherTestconfigurationDir = Path.Combine(Environment.CurrentDirectory, "AnotherTestConfiguration\\ApplicationX");
            Assert.That(Directory.Exists(anotherTestconfigurationDir), "Test data does not exists");
            String redirectFile = Path.Combine(Environment.CurrentDirectory, "Configuration.Sample\\ApplicationX.redirect");
            File.WriteAllText(redirectFile, anotherTestconfigurationDir);
            var result = client.DownloadString(baseUri + "/ApplicationX");
            JArray setting = (JArray)JsonConvert.DeserializeObject(result);
            Assert.That(setting.Count, Is.EqualTo(1));
        }

        [Test]
        public void redirect_of_folder_is_working_with_specific_hosts()
        {
            String anotherTestconfigurationDir = Path.Combine(Environment.CurrentDirectory, "AnotherTestConfiguration\\ApplicationX");
            Assert.That(Directory.Exists(anotherTestconfigurationDir), "Test data does not exists");
            String redirectFile = Path.Combine(Environment.CurrentDirectory, "Configuration.Sample\\ApplicationX.redirect");
            File.WriteAllText(redirectFile, anotherTestconfigurationDir);
            var result = client.DownloadString(baseUri + "/ApplicationX/ServiceY.config/HostA");
            JObject setting = (JObject)JsonConvert.DeserializeObject(result);
            Assert.That((String)setting["ServiceName"], Is.EqualTo("Y for HostA")); //specific configuration

            Assert.That((String)setting["workers"], Is.EqualTo("42")); //applicationX configuration   
        }

        [Test]
        public void redirect_of_folder_is_using_same_base_service_for_redirection()
        {
            String anotherTestconfigurationDir = Path.Combine(Environment.CurrentDirectory, "AnotherTestConfiguration\\ApplicationX");
            Assert.That(Directory.Exists(anotherTestconfigurationDir), "Test data does not exists");
            String redirectFile = Path.Combine(Environment.CurrentDirectory, "Configuration.Sample\\ApplicationX.redirect");
            File.WriteAllText(redirectFile, anotherTestconfigurationDir);
            var result = client.DownloadString(baseUri + "/ApplicationX/ServiceY.config");
            JObject setting = (JObject)JsonConvert.DeserializeObject(result);

            Assert.That(
                    (String)setting["message"],
                    Is.EqualTo("hello from Configuration Server"),
                    "Even with redirectrion, base config.json is the one located in the root of the configuration server"
                );  
        }
    }
}
