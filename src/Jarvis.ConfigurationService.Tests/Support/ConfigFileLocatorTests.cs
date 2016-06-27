using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jarvis.ConfigurationService.Host.Support;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Jarvis.ConfigurationService.Tests.Support
{
    [TestFixture]
    public class ConfigFileLocatorTests
    {
        [Test]
        public void Compose_empty_return_empty()
        {
            var composed = JsonComposer.ComposeJsonContent();
            Assert.That(composed, Is.EqualTo(null));
        }

        [Test]
        public void verify_basic_composition_of_unrelated_properties()
        {
            var json1 = "{prop : 'test'}";
            var json2 = "{otherProp : 'anotherTest'}";

            var result = JsonComposer.ComposeJsonContent(json1, json2);

            Assert.That((String) result["prop"], Is.EqualTo("test"));
            Assert.That((String)result["otherProp"], Is.EqualTo("anotherTest"));
        }

        [Test]
        public void verify_priority()
        {
            var json1 = "{prop : 'test'}";
            var json2 = "{prop : 'test1'}";

            var result = JsonComposer.ComposeJsonContent(json1, json2);
            Assert.That((String)result["prop"], Is.EqualTo("test1"));

            result = JsonComposer.ComposeJsonContent(json2, json1);
            Assert.That((String)result["prop"], Is.EqualTo("test"));
        }

        [Test]
        public void verify_with_empty_object_priority()
        {
            var json1 = "{}";
            var json2 = "{prop : 'test1'}";

            var result = JsonComposer.ComposeJsonContent(json1, json2);
            Assert.That((String)result["prop"], Is.EqualTo("test1"));

        }

        [Test]
        public void verify_order_of_properties()
        {
            var json1 = "{prop : 'test'}";
            var json2 = "{otherProp : 'anotherTest', prop : 'modified'}";

            var result = JsonComposer.ComposeJsonContent(json1, json2);

            Assert.That((String)result["prop"], Is.EqualTo("modified"));
        }

        [Test]
        public void verify_nested_single_property_override()
        {
            var json1 = "{prop : 'test', complexobj : {'prop1' : 1, 'prop2' : 2}}";
            var json2 = "{complexobj : {'prop2' : 42}}";

            var result = JsonComposer.ComposeJsonContent(json1, json2);
            var complexObject = (JObject)result["complexobj"];
            Assert.That((Int32)complexObject["prop1"], Is.EqualTo(1));
            Assert.That((Int32)complexObject["prop2"], Is.EqualTo(42));
        }

        [Test]
        public void verify_nested_two_level_single_property_override()
        {
            var json1 = "{prop : 'test', complexobj : { 'cp' : {'p1' : 5, 'p2' : 6}, 'prop1' : 1, 'prop2' : 2}}";
            var json2 = "{complexobj : {'cp' : {'p1' : 0}}}";

            var result = JsonComposer.ComposeJsonContent(json1, json2);
            var complexObject = (JObject)result["complexobj"];
            Assert.That((Int32)complexObject["prop1"], Is.EqualTo(1));
            Assert.That((Int32)complexObject["prop2"], Is.EqualTo(2));
            var complexObjectNested = (JObject)complexObject["cp"];
            Assert.That((Int32)complexObjectNested["p1"], Is.EqualTo(0));
            Assert.That((Int32)complexObjectNested["p2"], Is.EqualTo(6));
        }
    }
}
