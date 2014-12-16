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
using NSubstitute;
using System.Configuration;

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
            _app = WebApp.Start<ConfigurationServiceApplication>(baseUri);
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
            Assert.That(result, Is.StringContaining(@"Applications"":[""MyApp1"",""MyApp2"",""MyAppTest"",""myapp3""]"));
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
            Assert.That(result, Is.EqualTo(@"{""connectionStrings"":{""bl"":""mongodb://localhost/bl"",""log"":""mongodb://localhost/log-service1""},""message"":""hello from service 2"",""instruction"":""This is the base configuration file for the entire MyApp1 application"",""workers"":""1"",""baseSetting"":""hello world from service 2"",""enableApi"":""false""}"));
        }

        [Test]
        public void correct_configuration_of_single_service_with_old_route()
        {
            var result = client.DownloadString(baseUri + "/MyApp1/Service2/config.json");
            Assert.That(result, Is.EqualTo(@"{""connectionStrings"":{""bl"":""mongodb://localhost/bl"",""log"":""mongodb://localhost/log-service1""},""message"":""hello from service 2"",""instruction"":""This is the base configuration file for the entire MyApp1 application"",""workers"":""1"",""baseSetting"":""hello world from service 2"",""enableApi"":""false""}"));
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
        public void client_is_informed_when_malformed_json_is_entered()
        {
            try
            {
                var result = client.DownloadString(baseUri + "/MyAppTest/ServiceMalformed.config");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return;
            }
            Assert.Fail("An exception should be generated");
        }

        [Test]
        public void malformed_json_return_error_with_json_payload()
        {
            try
            {
                var result = client.DownloadString(baseUri + "/MyAppTest/ServiceMalformed.config");
                Assert.Fail("An exception should be generated");
            }
            catch (WebException ex)
            {

                var responseStream = ex.Response.GetResponseStream();

                using (var reader = new StreamReader(responseStream))
                {
                    var responseText = reader.ReadToEnd();
                    JObject obj = (JObject) JsonConvert.DeserializeObject(responseText);
                    Assert.That((String) obj["ExceptionMessage"], Contains.Substring("Unterminated string"));
                }
            }
        }

        [Test]
        public void verify_exception_with_malformed_json_is_entered()
        {
            try
            {
                var result = client.DownloadString(baseUri + "/MyAppTest/ServiceMalformed.config");
                Assert.Fail("An exception should be generated");
            }
            catch (WebException ex)
            {
                if (ex.Response != null)
                {
                    var responseStream = ex.Response.GetResponseStream();

                    if (responseStream != null)
                    {
                        using (var reader = new StreamReader(responseStream))
                        {
                            var responseText = reader.ReadToEnd();
                            Assert.That(responseText, Contains.Substring("ServiceMalformed.config"));
                            Assert.That(responseText, Contains.Substring("MyAppTest"));
                        }
                    }
                }
                return;
            }
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
            Assert.That((String)resultObject["encryptedSetting"], Is.EqualTo("my password"));
        }

        [Test]
        public void verify_encrypted_settings_for_specific_host()
        {
            var result = client.DownloadString(baseUri + "/MyApp1/Service1.config/Host1");
            JObject resultObject = (JObject)JsonConvert.DeserializeObject(result);
            Assert.That((String)resultObject["encryptedSetting"], Is.EqualTo("password for host1"));
        }

        [Test]
        public void empty_config_return_exception()
        {
            IFileSystem substitute = NSubstitute.Substitute.For<IFileSystem>();
            using (FileSystem.Override(substitute))
            {
                substitute.GetFileContent(null).ReturnsForAnyArgs("");
                substitute.GetBaseDirectory().ReturnsForAnyArgs(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location));
                try
                {
                    //now there is no file returned by file system
                    var result = client.DownloadString(baseUri + "/MyApp1/Service1.config");
                    Assert.Fail("This call should raise Configuration Exception");
                }
                catch (WebException cex)
                {
                    //system shoudl throw exception
                }
            }
        }

        [Test]
        public void verify_encrypted_settings_for_not_overridden_property()
        {
            var result = client.DownloadString(baseUri + "/MyApp1/Service1.config/Host1");
            JObject resultObject = (JObject)JsonConvert.DeserializeObject(result);
            Assert.That((String)resultObject["otherEncryptedSetting"], Is.EqualTo("secured"));
        }


        [Test]
        public void verify_base_config_in_default_folder()
        {
            var result = client.DownloadString(baseUri + "/MyApp1/service1.config");
            JObject jobj = (JObject)JsonConvert.DeserializeObject(result);
            Assert.That((String)jobj["baseSetting"], Is.EqualTo("hello world"), "Base.config not used in default directory");
        }

        [Test]
        public void verify_base_config_in_host_specific_folder()
        {
            var result = client.DownloadString(baseUri + "/MyApp1/service1.config/host1");
            JObject jobj = (JObject)JsonConvert.DeserializeObject(result);
            Assert.That((String)jobj["baseSetting"], Is.EqualTo("hello world host 1"), "Base.config specific host not correctly used");
        }

        [Test]
        public void verify_base_config_in_default_folder_has_less_precedence_than_specific()
        {
            var result = client.DownloadString(baseUri + "/MyApp1/service2.config");
            JObject jobj = (JObject)JsonConvert.DeserializeObject(result);
            Assert.That((String)jobj["baseSetting"], Is.EqualTo("hello world from service 2"), "Base.config has less precedence than specific settings");
        }

        [Test]
        public void verify_invalid_encryption_return_unencrypted()
        {
            var result = client.DownloadString(baseUri + "/MyApp1/Service1.config/Host2");
            JObject resultObject = (JObject)JsonConvert.DeserializeObject(result);
            Assert.That((String)resultObject["Host2Specific"], Is.EqualTo("test"));
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

        [Test]
        public void handling_of_simple_resource_for_entire_application()
        {
            var resFile = client.DownloadString(baseUri + "/MyApp1/resources/ServiceY/resourceFile.Xml/hostnonexisting");
            Assert.That(resFile, Is.EqualTo(
@"<root>
  <node value=""test"" />
</root>"));
        }

        [Test]
        public void host_override_for_simple_resource_for_entire_application()
        {
            var resFile = client.DownloadString(baseUri + "/MyApp1/resources/ServiceY/resourceFile.Xml/Host1");
            Assert.That(resFile, Is.EqualTo(
@"<root>
  <node value=""this is for host1"" />
</root>"));
        }

        [Test]
        public void handling_of_simple_resource_for_specific_application()
        {
            var resFile = client.DownloadString(baseUri + "/MyApp1/resources/Service1/resourceFile.Xml/hostnonexisting");
            Assert.That(resFile, Is.EqualTo(
@"<root>
  <node value=""testForService1"" />
</root>"));
        }
    }
}
