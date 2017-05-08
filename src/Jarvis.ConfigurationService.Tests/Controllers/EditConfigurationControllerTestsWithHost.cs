using Jarvis.ConfigurationService.Host.Model;
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
    public class EditConfigurationControllerTestsWithHost
    {
        private IDisposable _app;
        private TestWebClient client;
        private readonly String baseUri = "http://localhost:53642";

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
        public void Verify_registration_of_new_application()
        {
            var newId = Guid.NewGuid().ToString();
            AddApplication parameter = new AddApplication()
            {
                ApplicationName = newId,
                RedirectFolder = @"C:\temp\test\" + newId
            };
            var serializedParameter = JsonConvert.SerializeObject(parameter);
            var result = client.DownloadString(
                baseUri + "/api/applications/" + newId,
                serializedParameter,
                "PUT");
            JObject jobj = (JObject)JsonConvert.DeserializeObject(result);
            Assert.That((Boolean)jobj["success"], Is.EqualTo(true), "Operation completed with errors");
            var applicationResult = client.DownloadString(baseUri + "/status");
            JObject jobjAppResults = (JObject)JsonConvert.DeserializeObject(applicationResult);
            var applications = ((JArray) jobjAppResults["Applications"]).Select(t => t.Value<String>()).ToList();

            Assert.That(applications.Contains(newId), "Application was not correctly created");
        }

        [Test]
        public void Verify_error_when_application_is_already_present()
        {
            var applicationResult = client.DownloadString(baseUri + "/status");
            JObject jobjAppResults = (JObject)JsonConvert.DeserializeObject(applicationResult);
            var applications = ((JArray)jobjAppResults["Applications"]).Select(t => t.Value<String>()).ToList();

            var existingApp = applications[0];
            AddApplication parameter = new AddApplication()
            {
                ApplicationName = existingApp,
                RedirectFolder = @"C:\temp\test\" + existingApp
            };
            var serializedParameter = JsonConvert.SerializeObject(parameter);
            var result = client.DownloadString(
                baseUri + "/api/applications/" + existingApp,
                serializedParameter,
                "PUT");
            JObject jobj = (JObject)JsonConvert.DeserializeObject(result);
            Assert.That((Boolean)jobj["success"], Is.EqualTo(false), "Operation should file because application already exists");
        }

        [Test]
        public void Verify_error_when_application_is_created_two_times()
        {
            var newId = Guid.NewGuid().ToString();
            AddApplication parameter = new AddApplication()
            {
                ApplicationName = newId,
                RedirectFolder = @"C:\temp\test\" + newId
            };
            var serializedParameter = JsonConvert.SerializeObject(parameter);
            var result = client.DownloadString(
                baseUri + "/api/applications/" + newId,
                serializedParameter,
                "PUT");
            JObject jobj = (JObject)JsonConvert.DeserializeObject(result);
            Assert.That((Boolean)jobj["success"], Is.EqualTo(true), "Operation completed with errors");

            result = client.DownloadString(
               baseUri + "/api/applications/" + newId,
               serializedParameter,
               "PUT");
            jobj = (JObject)JsonConvert.DeserializeObject(result);
            Assert.That((Boolean)jobj["success"], Is.EqualTo(false), "Operation completed with errors");
        }
    }
}
