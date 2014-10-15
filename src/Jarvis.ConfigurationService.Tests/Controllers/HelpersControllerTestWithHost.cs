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
        public void Verify_generation_of_new_encryption_key()
        {
            JObject parsed = CallController("/support/encryption/generatekey");
            Assert.That(parsed["IV"], Is.Not.Null);
            Assert.That(parsed["Key"], Is.Not.Null);
        }

        private JObject CallController(String path)
        {
            var result = client.DownloadString(baseUri + path);
            JObject parsed = (JObject)JsonConvert.DeserializeObject(result);
            return parsed;
        }

       
    }
}
