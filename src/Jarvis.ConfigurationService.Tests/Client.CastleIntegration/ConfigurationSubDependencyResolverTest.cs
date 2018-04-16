using Castle.Windsor;
using Jarvis.ConfigurationService.Client;
using Jarvis.ConfigurationService.Client.CastleIntegration;
using Jarvis.ConfigurationService.Client.Support;
using Jarvis.ConfigurationService.Tests.Support;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NSubstitute;
using Castle.MicroKernel.Registration;

namespace Jarvis.ConfigurationService.Tests.Client.CastleIntegration
{
    [TestFixture]
    public class ConfigurationSubDependencyResolverTest
    {

        private IEnvironment stubEnvironment;
        private IDisposable cleanup;

        [SetUp]
        public void SetUp()
        {
            stubEnvironment = NSubstitute.Substitute.For<IEnvironment>();

            //valid directory as base test execution path.
            stubEnvironment.GetCurrentPath().Returns(@"c:\develop\blabla\nameofsoftware\src\blabla\bin\debug");

            stubEnvironment.GetFileContent("", false).ReturnsForAnyArgs(String.Empty);
            stubEnvironment.GetEnvironmentVariable("").ReturnsForAnyArgs("http://localhost");
            ClientConfiguration config = new ClientConfiguration(@"#jarvis-config
application-name : TESTAPPLICATION
base-server-address : http://localhost:55555/", "C:\\temp\\TESTAPPLICATION.config");
            stubEnvironment.GetApplicationConfig().ReturnsForAnyArgs(config);
            stubEnvironment.GetMachineName().ReturnsForAnyArgs("TestMachine");
        }

        private void CreateConfigurationClientForTest()
        {

            var testLogger = new TestLogger();
            var client = new ConfigurationServiceClient(testLogger.Log, "CQRS_TEST_CONFIGURATION_MANAGER", stubEnvironment);
            cleanup = ConfigurationServiceClient.OverrideInstanceForUnitTesting(client);
        }

        [TearDown]
        public void TearDown()
        {
            cleanup.Dispose();
        }

        [Test]
        public void resolve_dependency_on_settings_constructor()
        {
            stubEnvironment.DownloadFile("").ReturnsForAnyArgs("{ 'setting1' : 'value1', 'setting2' : 'value2'}");
            CreateConfigurationClientForTest();
            WindsorContainer container = new WindsorContainer();
            ConfigurationSubDependencyResolver.InstallIntoContainer(container);
            container.Register(Component.For<TestDependencyFromConfigurationClass>().ImplementedBy<TestDependencyFromConfigurationClass>());

            var c = container.Resolve<TestDependencyFromConfigurationClass>();
            Assert.That(c.Setting1, Is.EqualTo("value1"));
        }

        [Test]
        public void resolve_dependency_on_settings_properties()
        {
            stubEnvironment.DownloadFile("").ReturnsForAnyArgs("{ 'setting1' : 'value1', 'Setting2' : 'value2'}");
            CreateConfigurationClientForTest();
            WindsorContainer container = new WindsorContainer();
            ConfigurationSubDependencyResolver.InstallIntoContainer(container);
            container.Register(Component.For<TestDependencyFromConfigurationClass>().ImplementedBy<TestDependencyFromConfigurationClass>());

            var c = container.Resolve<TestDependencyFromConfigurationClass>();
            Assert.That((String) c.Setting2, Is.EqualTo("value2"));
        }

        #region Test Classes

        public class TestDependencyFromConfigurationClass 
        {
            public String Setting1 { get { return _setting1; } }
            private String _setting1;

            public TestDependencyFromConfigurationClass(StringConfiguration setting1)
            {
                _setting1 = setting1.Value;
            }

            public StringConfiguration Setting2 { get; set; }
        }

        #endregion

    }
}
