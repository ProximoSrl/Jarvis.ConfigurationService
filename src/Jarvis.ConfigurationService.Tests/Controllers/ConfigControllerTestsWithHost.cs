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
        TestWebClient client;
        String baseUri = "http://localhost:53642";

        [TestFixtureSetUp]
        public void FixtureSetup()
        {
            _app = WebApp.Start<ConfigurationServiceApplication>(baseUri);
            client = new TestWebClient();
        }

        [TestFixtureTearDown]
        public void FixtureTearDown()
        {
            _app.Dispose();

        }

        [Test]
        public void Smoke_test_root()
        {
            var result = client.DownloadString(baseUri+ "/status");
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
            var result = client.DownloadString(baseUri + "/status");
            Assert.That(result, Is.StringContaining(@"Applications"":[""MyApp1"",""MyApp2"",""MyAppParam"",""MyAppTest"",""OverrideTest"",""myapp3""]"));
        }

        [Test]
        public void correct_listing_of_all_services_in_application()
        {
            var result = client.DownloadString(baseUri + "/MyApp1/status");
            Assert.That(result, Is.StringContaining(@"[""Service1"",""Service2""]"));
        }

        String expected = @"{""connectionStrings"":{""bl"":""mongodb://localhost/bl"",""log"":""mongodb://localhost/log-service1""},""message"":""hello from service 2"",""simple-parameter-setting-root"":""100"",""complex-parameter-setting-root"":""42"",""instruction"":""This is the base configuration file for the entire MyApp1 application"",""workers"":""1"",""simple-parameter-setting"":""100"",""complex-parameter-setting"":""42"",""baseSetting"":""hello world from service 2"",""enableApi"":""false""}";
        [Test]
        public void correct_configuration_of_single_service()
        { 
            var result = client.DownloadString(baseUri + "/MyApp1/Service2.config");
            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void correct_configuration_of_single_service_with_old_route()
        {
            var result = client.DownloadString(baseUri + "/MyApp1/Service2/config.json");
            Assert.That(result, Is.EqualTo(expected));
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
            var thisClient = new WebClient();
            try
            {
                var result = thisClient.DownloadString(baseUri + "/MyAppTest/ServiceMalformed.config");
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
            var thisClient = new WebClient();
            try
            {
                var result = thisClient.DownloadString(baseUri + "/MyAppTest/ServiceMalformed.config");
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
            var result = client.DownloadString(baseUri + "/MyApp1/Service1.config/Host1");
            JObject jobj = (JObject)JsonConvert.DeserializeObject(result);
            Assert.That((String)jobj["baseSetting"], Is.EqualTo("hello world host 1"), "Base.config specific host not correctly used");
        }

        [Test]
        public void verify_base_config_in_default_folder_has_less_precedence_than_specific()
        {
            var result = client.DownloadString(baseUri + "/MyApp1/Service2.config");
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
            String anotherTestconfigurationDir = Path.Combine(Environment.CurrentDirectory, "AnotherTestConfiguration","ApplicationX");
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
            String anotherTestconfigurationDir = Path.Combine(Environment.CurrentDirectory, "AnotherTestConfiguration","ApplicationX");
            Assert.That(Directory.Exists(anotherTestconfigurationDir), "Test data does not exists");
            String redirectFile = Path.Combine(Environment.CurrentDirectory, "Configuration.Sample","ApplicationX.redirect");
            File.WriteAllText(redirectFile, anotherTestconfigurationDir);
            var result = client.DownloadString(baseUri + "/ApplicationX/status");
            JArray setting = (JArray)JsonConvert.DeserializeObject(result);
            Assert.That(setting.Count, Is.EqualTo(1));
        }

        [Test]
        public void redirect_of_folder_is_working_with_specific_hosts()
        {
            String anotherTestconfigurationDir = Path.Combine(Environment.CurrentDirectory, "AnotherTestConfiguration","ApplicationX");
            Assert.That(Directory.Exists(anotherTestconfigurationDir), "Test data does not exists");
            String redirectFile = Path.Combine(Environment.CurrentDirectory, "Configuration.Sample","ApplicationX.redirect");
            File.WriteAllText(redirectFile, anotherTestconfigurationDir);
            var result = client.DownloadString(baseUri + "/ApplicationX/ServiceY.config/HostA");
            JObject setting = (JObject)JsonConvert.DeserializeObject(result);
            Assert.That((String)setting["ServiceName"], Is.EqualTo("Y for HostA")); //specific configuration

            Assert.That((String)setting["workers"], Is.EqualTo("42")); //applicationX configuration   
        }

        [Test]
        public void redirect_of_folder_is_using_same_base_service_for_redirection()
        {
            String anotherTestconfigurationDir = Path.Combine(Environment.CurrentDirectory, "AnotherTestConfiguration","ApplicationX");
            Assert.That(Directory.Exists(anotherTestconfigurationDir), "Test data does not exists");
            String redirectFile = Path.Combine(Environment.CurrentDirectory, "Configuration.Sample","ApplicationX.redirect");
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
        public void redirect_of_folder_support_overriding_with_local_directory()
        {
            String anotherTestconfigurationDir = Path.Combine(Environment.CurrentDirectory, "AnotherTestConfiguration", "ApplicationX");
            
            String redirectFile = Path.Combine(Environment.CurrentDirectory, "Configuration.Sample", "OverrideTest.redirect");
            File.WriteAllText(redirectFile, anotherTestconfigurationDir);
            var result = client.DownloadString(baseUri + "/OverrideTest/anyservice.config");
            JObject setting = (JObject)JsonConvert.DeserializeObject(result);
            Assert.That((String)setting["connectionStringsX"]["operational"],
                Is.EqualTo("mongodb://localhost/operational"),
                "operational connection string exists only in the redirect folder and should win");
            Assert.That((String)setting["connectionStringsX"]["log"],
                    Is.EqualTo("mongodb://localhost/this-should-win"),
                    "base folder must win over redirection");
            Assert.That((String)setting["workers"],Is.EqualTo("1000"), "base folder must win over redirection");
        }

        [Test]
        public void handling_of_simple_resource_for_entire_application()
        {
            var resFile = client.DownloadString(baseUri + "/MyApp1/resources/ServiceY/resourceFile.xml/hostnonexisting");
            Assert.That(resFile, Is.EqualTo(
@"<root>
  <node value=""test"" />
</root>"));
        }

        [Test]
        public void host_override_for_simple_resource_for_entire_application()
        {
            var resFile = client.DownloadString(baseUri + "/MyApp1/resources/ServiceY/resourceFile.xml/Host1");
            Assert.That(resFile, Is.EqualTo(
@"<root>
  <node value=""this is for host1"" />
</root>"));
        }

        [Test]
        public void handling_of_simple_resource_for_specific_application()
        {
            var resFile = client.DownloadString(baseUri + "/MyApp1/resources/Service1/resourceFile.xml/hostnonexisting");
            Assert.That(resFile, Is.EqualTo(
@"<root>
  <node value=""testForService1"" />
</root>"));
        }

        [Test]
        public void handling_of_simple_resource_for_specific_application_and_host()
        {
            var resFile = client.DownloadString(baseUri + "/MyApp1/resources/Service1/resourceFile.xml/Host1");
            Assert.That(resFile, Is.EqualTo(
@"<root>
  <node value=""host 1 and service 1"" />
</root>"));
        }

        [Test]
        public void override_parameters_for_service_with_base_app_parameter()
        {
            var result = client.DownloadString(baseUri + "/MyAppParam/service1.config/Host1");
            JObject jobj = (JObject)JsonConvert.DeserializeObject(result);
            Assert.That((String)jobj["override-parameter-test"], Is.EqualTo("104"), "Override parameters with base parameters failed");
        }
          
        [Test]
        public void override_parameters_for_service_with_service_parameter_file()
        {
            var result = client.DownloadString(baseUri + "/MyAppParam/service1.config/Host1");
            JObject jobj = (JObject)JsonConvert.DeserializeObject(result);
            Assert.That((String)jobj["override-parameter-complex"], Is.EqualTo("test"), "Override parameters with service parameters failed");
        }

        [Test] 
        public void override_parameters_for_service_with_service_parameter_file_not_change_other_properteis()
        {
            var result = client.DownloadString(baseUri + "/MyAppParam/service1.config/Host1");
            JObject jobj = (JObject)JsonConvert.DeserializeObject(result);
            Assert.That((String)jobj["subparam2-parameter"], Is.EqualTo("43"), "overriding complex parameters leaves sibling unaltered");
        }
         
        [Test]
        public void override_parameters_with_partial()
        {
            var result = client.DownloadString(baseUri + "/MyAppParam/service1.config/Host1");
            JObject jobj = (JObject)JsonConvert.DeserializeObject(result);
            Assert.That((String)jobj["partial-parameter"], Is.EqualTo("this is 104 partial"), "parameters supports partial substitution");
        }

         [Test]
        public void override_parameters_with_multiple_partial_and_nested()
        {
            var result = client.DownloadString(baseUri + "/MyAppParam/service1.config/Host1");
            JObject jobj = (JObject)JsonConvert.DeserializeObject(result);
            Assert.That((String)jobj["partial-multiple-parameter"], Is.EqualTo("this is 104 partial I want also test"), "parameters supports partial substitution");
        }


        [Test]
        public void nested_parameter_support()
        {
            var result = client.DownloadString(baseUri + "/MyAppParam/service1.config/Host1");
            JObject jobj = (JObject)JsonConvert.DeserializeObject(result);
            Assert.That((String)jobj["nested-settings"], Is.EqualTo("Composed with final nested value"), "parameter contains other parameter settings");
        }

        [Test]
        public void escape_support()
        {
            var result = client.DownloadString(baseUri + "/MyAppParam/service1.config/Host1");
            JObject jobj = (JObject)JsonConvert.DeserializeObject(result);
            Assert.That((String)jobj["escape-parameter-char"], Is.EqualTo("this settings contains % char"), "percentage char is escaped with %%");
            Assert.That((String)jobj["double-escape-parameter-char"], Is.EqualTo("this settings contains % char and I'm expecting % not to be altered"), "percentage char is escaped with %%");
        
        }
          
        [Test]
        public void base_system_parameters()
        {
            var result = client.DownloadString(baseUri + "/MyAppParam/service1.config/Host1");
            JObject jobj = (JObject)JsonConvert.DeserializeObject(result);
            Assert.That((String)jobj["paramTest"], Is.EqualTo("this use MyAppParam in config"), "Usage of base sys.appName parameter does not work");
        }

        [Test]
        public void cyclic_parameter_substitution_with_parameterse()
        {
            var result = client.DownloadString(baseUri + "/MyAppParam/service1.config/Host1");
            JObject jobj = (JObject)JsonConvert.DeserializeObject(result);
            Assert.That((String)jobj["readmodel"], Is.EqualTo("mongodb://localhost/MyAppParam-readmodel"), "Usage of base sys.appName parameter in parameters definition does not work");
        }

        [Test] 
        public void missing_parameter_should_throw()
        {
            var thisClient = new WebClient();
            try
            {
                thisClient.DownloadString(baseUri + "/MyAppParam/servicemalformed.config/Host1");
            }
            catch (WebException ex)
            {

                var responseStream = ex.Response.GetResponseStream();

                using (var reader = new StreamReader(responseStream))
                {
                    var responseText = reader.ReadToEnd();
                    JObject obj = (JObject)JsonConvert.DeserializeObject(responseText);
                    Assert.That((String)obj["ExceptionMessage"], Contains.Substring("missing-parameter"));
                }
                return;
            }  
            Assert.Fail("Should throw because a parameter is missing");
         }

        [Test]
        public void redirected_parameters_should_not_win()
        {
            String anotherTestconfigurationDir = Path.Combine(Environment.CurrentDirectory, "AnotherTestConfiguration", "ApplicationX");

            String redirectFile = Path.Combine(Environment.CurrentDirectory, "Configuration.Sample", "OverrideTest.redirect");
            File.WriteAllText(redirectFile, anotherTestconfigurationDir);
            var result = client.DownloadString(baseUri + "/OverrideTest/anyservice.config");
            JObject setting = (JObject)JsonConvert.DeserializeObject(result);

            Assert.That((String)setting["with-param"], Is.EqualTo("override-value"), "Param in non-redirected folder should win");
        } 
    }
}
