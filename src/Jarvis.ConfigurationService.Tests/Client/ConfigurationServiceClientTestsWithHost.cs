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
using Jarvis.ConfigurationService.Tests.Support;
using Jarvis.ConfigurationService.Host.Support;
using Microsoft.Owin.Hosting;

namespace Jarvis.ConfigurationService.Tests.Client
{
    [TestFixture] 
    public class ConfigurationServiceClientTestsWithHost
    {
        IDisposable _app;
        TestWebClient client;
        String baseUri = "http://localhost:53642";
        ConfigurationServiceClient sut;

        [OneTimeSetUp]
        public void FixtureSetup()
        {
            var baseDir = AppDomain.CurrentDomain.SetupInformation.ApplicationBase;
            var previousDir = new DirectoryInfo(baseDir + "/../../");
            var fileName = new FileInfo(Path.Combine(previousDir.FullName, "testappname.application"));
            if (File.Exists(fileName.FullName))
            {
                File.Delete(fileName.FullName);
            }
            File.WriteAllText(fileName.FullName,
@"#jarvis-configuration
application-name : MyApp1
base-server-address : http://localhost:53642/");

            _app = WebApp.Start<ConfigurationServiceApplication>(baseUri);
            client = new TestWebClient();
        }

        [OneTimeTearDown]
        public void FixtureTearDown()
        {
            _app.Dispose();

        }

        [SetUp]
        public void SetUp() {

            
        }

        private void CreateStandardSut()
        {
            sut = new ConfigurationServiceClient(
                (s, b, ex) => Console.WriteLine(s),
                "TEST",
                new StandardEnvironment());
        }

     

        [Test]
        public void simple_call_smoke_test() 
        {
            CreateStandardSut();
            var result = sut.GetSetting("baseSetting");
            Assert.That(result, Is.EqualTo("hello world"));
        }      
    }

    [TestFixture]
    public class ConfigurationServiceClientTestsWithHostAndParams
    {
        IDisposable _app;
        TestWebClient client;
        String baseUri = "http://localhost:53642";
        ConfigurationServiceClient sut;

        [OneTimeSetUp]
        public void FixtureSetup()
        {
            var baseDir = AppDomain.CurrentDomain.SetupInformation.ApplicationBase;
            var previousDir = new DirectoryInfo(baseDir + "/../../");
            var fileName = new FileInfo(Path.Combine(previousDir.FullName, "testappname.application"));
            if (File.Exists(fileName.FullName))
            {
                File.Delete(fileName.FullName);
            }
            File.WriteAllText(fileName.FullName,
@"#jarvis-configuration
application-name : MyAppParam
base-server-address : http://localhost:53642/");

            _app = WebApp.Start<ConfigurationServiceApplication>(baseUri);
            client = new TestWebClient();
        }

        [OneTimeTearDown]
        public void FixtureTearDown()
        {
            _app.Dispose();

        }

        [SetUp]
        public void SetUp()
        {


        }

        private void CreateSutWithDefault(FileInfo defaultConfig, FileInfo defaultParametersConfig)
        {
            sut = new ConfigurationServiceClient(
                (s, b, ex) => Console.WriteLine(s),
                "TEST",
                new StandardEnvironment(),
                defaultConfig,
                defaultParametersConfig);
        }

        [Test]
        public void call_for_default_smoke_test()
        {
            FileInfo defConf = new FileInfo("Client\\base.config");
            FileInfo defParam = new FileInfo("Client\\parameters.config");
            CreateSutWithDefault(defConf, defParam);
            var result = sut.GetSetting("default-setting");
            Assert.That(result, Is.EqualTo("def-value"));

            result = sut.GetSetting("default-setting-param");
            Assert.That(result, Is.EqualTo("def-value:default-param"));
        }

        [Test]
        public void default_parameters_has_lower_precedence()
        {
            FileInfo defConf = new FileInfo("Client\\base.config");
            FileInfo defParam = new FileInfo("Client\\parameters.config");
            CreateSutWithDefault(defConf, defParam);
            var result = sut.GetSetting("overridden");
            Assert.That(result, Is.EqualTo("overriddenValue"));

        }

        [Test]
        public void create_new_application()
        {
            FileInfo defConf = new FileInfo("Client\\base.config");
            FileInfo defParam = new FileInfo("Client\\parameters.config");
            CreateSutWithDefault(defConf, defParam);
            var appId = Guid.NewGuid().ToString();
            var result = sut.AddApplication(new ConfigurationService.Client.ClientModels.AddApplication()
            {
                ApplicationName = appId,
                RedirectFolder = "c:\\test\\" + appId
            });
            Assert.That(result, Is.True);
        }
    }
}
