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
    public class HelpersControllerTestWithHost
    {
        IDisposable _app;
        String baseUri = "http://localhost:53642";

        [TestFixtureSetUp]
        public void FixtureSetup() 
        {
            _app = WebApp.Start<Startup>(baseUri);
        }

        [TestFixtureTearDown]
        public void FixtureTearDown()
        {
            _app.Dispose();
        }

        [Test]
        public void Verify_generation_of_new_encryption_key()
        {
            JObject parsed = CallControllerJsonResult("/support/encryption/generatekey");
            Assert.That(parsed["IV"], Is.Not.Null);
            Assert.That(parsed["Key"], Is.Not.Null);
        }


        [Test]
        public void Verify_encryption_utils()
        {
            JObject parsed = CallControllerPostJsonResult("/support/encryption/encrypt", "{StringToEncrypt : 'pippo'}");
            Assert.That((Boolean) parsed["success"], Is.EqualTo(true));
            Assert.That((String) parsed["encrypted"], Is.Not.Null);
        }

        private JObject CallControllerJsonResult(String path)
        {
            using (WebClient client = new WebClient())
            {
                var result = client.DownloadString(baseUri + path);
                JObject parsed = (JObject)JsonConvert.DeserializeObject(result);
                return parsed;
            }
        }

        private JObject CallControllerPostJsonResult(String path, String jsonPayload)
        {
            using (WebClient client = new WebClient())
            {
                client.Headers[HttpRequestHeader.ContentType] = "application/json";
                string result = client.UploadString(baseUri + path, jsonPayload);
                JObject parsed = (JObject)JsonConvert.DeserializeObject(result);
                return parsed;
            }
        }
    }
}
