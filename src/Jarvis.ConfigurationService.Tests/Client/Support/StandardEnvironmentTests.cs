using Jarvis.ConfigurationService.Client.Support;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jarvis.ConfigurationService.Tests.Client.Support
{
    [TestFixture]
    public class StandardEnvironmentTests
    {
        StandardEnvironment _sut;

        [SetUp]
        public void SetUp()
        {
            _sut = new StandardEnvironment();
        }

        [Test]
        public void Save_without_encryption()
        {
            String fileName = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            _sut.SaveFile(fileName, "This is content", false, false);

            var content = File.ReadAllText(fileName);
            Assert.That(content, Is.EqualTo("This is content"));
        }

        [Test]
        public void Basic_file_Encryption()
        {
            String fileName = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            _sut.SaveFile(fileName, "This is content", false, true);

            var content = File.ReadAllText(fileName);
            Assert.That(content, Is.Not.EqualTo("This is content"));

            content = _sut.GetFileContent(fileName, true);
            Assert.That(content, Is.EqualTo("This is content"));
        }
    }
}
