using Jarvis.ConfigurationService.Host.Support;
using NUnit.Framework;
using System;
using System.IO;

namespace Jarvis.ConfigurationService.Tests.Support
{
    [TestFixture]
    public class StandardFileSystemTests
    {
        StandardFileSystem sut;

        [OneTimeSetUp]
        public void FixtureSetup()
        {
            sut = new StandardFileSystem(null);
        }

        [Test]
        public void Verify_folder_exists_on_redirection()
        {
            string tempFile = Path.ChangeExtension(Path.GetTempFileName(), ".redirect");
            String tempDirectory = Path.GetTempPath();
            File.WriteAllText(tempFile, tempDirectory);

            Assert.That(sut.DirectoryExists(Path.ChangeExtension(tempFile, "").TrimEnd('.'), true));
        }

        [Test]
        public void Verify_nested_folder_exists_on_redirection()
        {
            string tempFile = Path.ChangeExtension(Path.GetTempFileName(), "").TrimEnd('.');
            string redirectTempFile = tempFile + ".redirect";
            string otherDirectoryName = tempFile + "otherDirectory";
            String tempDirectory = Path.Combine(Path.GetTempPath(), otherDirectoryName);
            Directory.CreateDirectory(Path.Combine(tempDirectory, "NewDirectory"));
            File.WriteAllText(redirectTempFile, tempDirectory);

            var lastDirectory = Path.ChangeExtension(tempFile, "").TrimEnd('.');
            var nestedDirectory = Path.Combine(lastDirectory, "NewDirectory");
            Assert.That(sut.DirectoryExists(nestedDirectory, true));
            Assert.That(!sut.DirectoryExists(nestedDirectory, false));
        }
    }
}
