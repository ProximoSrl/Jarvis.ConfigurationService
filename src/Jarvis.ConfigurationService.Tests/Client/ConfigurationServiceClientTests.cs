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

namespace Jarvis.ConfigurationService.Tests.Client
{
    [TestFixture]
    public class ConfigurationServiceClientTests
    {
        private const String a_valid_configuration_file = "{ 'setting' : 'value', 'goodSetting' : 'hello setting' }";
        private const String an_invalid_configuration_file = "{ 'setting' , 'value' }";
        private const String a_valid_configuration_file_with_suboject = "{ 'setting' : 'value', 'connections' : { 'conn1' : 'valueconn1', 'conn2' : 'valueconn2'} }";
        private const String a_valid_configuration_file_with_nestedsuboject = @"{ 'setting' : 'value', 
        'connections' : {
            'mongo' : { 'conn1' : 'mongo1', 'conn2' : 'mongo2'},
            'sql' : { 'conn1' : 'sql1', 'conn4' : 'sql4'}
}}";

        private const String complex_object_configuration_file = @"{ 
'setting' : { 'propA' : 'valuea', 'propB' : 42},
'complex' : {
    'value' : 'test',
    'setting' : { 
        'propA' : 'valuea', 
        'propB' : 42
    } 
}

}";

        private const String array_configuration_file = @"{ 
'setting' : ['a', 'b', 'c'],
'complex' : {
        'value' : 'test1',
        'settings' : [
        { 
            'propA' : 'value1', 
            'propB' : 1
        },
        {
            'propA' : 'value2', 
            'propB' : 2
        }]
}}";

        private IEnvironment stubEnvironment;

        [SetUp]
        public void SetUp()
        {
            stubEnvironment = NSubstitute.Substitute.For<IEnvironment>();

            //valid directory as base test execution path.
            stubEnvironment.GetCurrentPath().Returns(@"c:\develop\blabla\nameofsoftware\src\blabla\bin\debug");

            stubEnvironment.DownloadFile("").ReturnsForAnyArgs(String.Empty);
            stubEnvironment.GetFileContent("").ReturnsForAnyArgs(String.Empty);
            stubEnvironment.GetEnvironmentVariable("").ReturnsForAnyArgs("http://localhost");
  
            stubEnvironment.GetApplicationName().ReturnsForAnyArgs("TESTAPPLICATION"); 
            stubEnvironment.GetMachineName().ReturnsForAnyArgs("TestMachine");
        }

        [Test]
        public void should_throw_exception_if_no_configuration_file_is_provided()
        {
            Assert.Throws<ConfigurationErrorsException>(() => CreateSut());
        }

        [Test]
        public void exception_for_no_configuration_file_found_should_tell_where_the_file_is_expected()
        {
            stubEnvironment.GetCurrentPath().Returns(@"c:\testpath\myprogram\myapp\");
            try
            {
                CreateSut();
                Assert.Fail("ConfigurationErrorsException is expected");
            }
            catch (ConfigurationErrorsException expectedException)
            {
                Assert.That(expectedException.Message, Contains.Substring("http://localhost/TESTAPPLICATION/myapp.config"));
            }

        }

        [Test]
        public void exception_if_no_valid_json_is_returned()
        {
            stubEnvironment.DownloadFile("").ReturnsForAnyArgs(an_invalid_configuration_file);
            stubEnvironment.GetCurrentPath().Returns(@"c:\testpath\myprogram\release\");
            try
            {
                CreateSut();
                Assert.Fail("ConfigurationErrorsException is expected");
            }
            catch (ConfigurationErrorsException expectedException)
            {
                Assert.That(expectedException.Message, Contains.Substring("json"));
            }

        }

        [Test]
        public void verify_correct_exception_handling()
        {
            stubEnvironment.DownloadFile("").ReturnsForAnyArgs(x => { throw new ConfigurationErrorsException("Malformed Json"); });
            stubEnvironment.GetCurrentPath().Returns(@"c:\testpath\myprogram\release\");
            try
            {
                CreateSut();
                Assert.Fail("ConfigurationErrorsException is expected");
            }
            catch (ConfigurationErrorsException expectedException)
            {
                Assert.That(expectedException.Message, Contains.Substring("Malformed Json"));
            }

        }

        [Test]
        public void exception_if_settings_not_present_and_no_default_value_specified()
        {
            stubEnvironment.DownloadFile("").ReturnsForAnyArgs(a_valid_configuration_file);
            try
            {
                var sut = CreateSut();
                sut.GetSetting("not_existing");
                Assert.Fail("ConfigurationErrorsException is expected");
            }
            catch (ConfigurationErrorsException expectedException)
            {
                Assert.That(expectedException.Message, Contains.Substring("not_existing"));
            }

        }

        [Test]
        public void exception_if_settings_not_present_and_no_default_value_specified_should_return_config_file_name()
        {
            stubEnvironment.DownloadFile("").ReturnsForAnyArgs(a_valid_configuration_file);
            stubEnvironment.GetCurrentPath().Returns(@"c:\testpath\myprogram\myapp\");
            try
            {
                var sut = CreateSut();
                sut.GetSetting("not_existing");
                Assert.Fail("ConfigurationErrorsException is expected");
            }
            catch (ConfigurationErrorsException expectedException)
            {
                Assert.That(expectedException.Message, Contains.Substring("http://localhost/TESTAPPLICATION/myapp.config"));
            }

        }

        [Test]
        public void configuration_manager_is_able_to_accept_testing_configuration_as_action()
        {
            stubEnvironment.DownloadFile("").ReturnsForAnyArgs(a_valid_configuration_file);
            stubEnvironment.GetCurrentPath().Returns(@"c:\testpath\myprogram\release\");

            var sut = CreateSut();
            sut.WithSetting("goodSetting", (setting) =>
            {
                //Doing something, and if something is wrong just raise an exception
            });
        }

        [Test]
        public void configuration_manager_is_able_to_accept_testing_configuration()
        {
            stubEnvironment.DownloadFile("").ReturnsForAnyArgs(a_valid_configuration_file);
            stubEnvironment.GetCurrentPath().Returns(@"c:\testpath\myprogram\myapp\");
            try
            {
                var sut = CreateSut();
                sut.WithSetting("goodSetting", (setting) =>
                {
                    throw new ApplicationException("TEST EXCEPTION");
                });
            }
            catch (ConfigurationErrorsException expectedException)
            {
                Assert.That(expectedException.Message, Contains.Substring("http://localhost/TESTAPPLICATION/myapp.config"));
                Assert.That(expectedException.Message, Contains.Substring("TEST EXCEPTION"));
                Assert.That(expectedException.Message, Contains.Substring("goodSetting"));
            }

        }

        [Test]
        public void configuration_manager_is_able_to_accept_testing_configuration_with_custom_error()
        {
            stubEnvironment.DownloadFile("").ReturnsForAnyArgs(a_valid_configuration_file);
            stubEnvironment.GetCurrentPath().Returns(@"c:\testpath\myprogram\myapp\");
            try
            {
                var sut = CreateSut();
                sut.WithSetting("goodSetting", (setting) => "error XXX");
            }
            catch (ConfigurationErrorsException expectedException)
            {
                Assert.That(expectedException.Message, Contains.Substring("http://localhost/TESTAPPLICATION/myapp.config"));
                Assert.That(expectedException.Message, Contains.Substring("error XXX"));
                Assert.That(expectedException.Message, Contains.Substring("goodSetting"));
            }

        }

        [Test]
        public void configuration_manager_does_not_duplicate_error_message_for_invalid_configuration()
        {
            stubEnvironment.DownloadFile("").ReturnsForAnyArgs(a_valid_configuration_file);
            stubEnvironment.GetCurrentPath().Returns(@"c:\testpath\myprogram\myapp\");
            try
            {
                var sut = CreateSut();
                sut.WithSetting("goodSetting", (setting) => "error XXX");
            }
            catch (ConfigurationErrorsException expectedException)
            {
                var matchCount =
                    Regex.Matches(expectedException.Message, "http://localhost/TESTAPPLICATION/myapp.config").Count;
                Assert.That(matchCount, Is.EqualTo(1));
            }

        }

        [Test]
        public void error_log_if_settings_not_present_and_no_default_value_specified()
        {
            stubEnvironment.DownloadFile("").ReturnsForAnyArgs(a_valid_configuration_file);
            try
            {
                var sut = CreateSut();
                sut.GetSetting("not_existing");
                Assert.Fail("ConfigurationErrorsException is expected");
            }
            catch (ConfigurationErrorsException expectedException)
            {
                //we want a log to be generated.
                Assert.That(currentTestLogger.ErrorCount, Is.GreaterThan(0));
            }

        }

        [Test]
        public void verify_correct_url_is_requested()
        {
            stubEnvironment.DownloadFile("").ReturnsForAnyArgs(a_valid_configuration_file);
            stubEnvironment.GetCurrentPath().Returns(@"c:\testpath\myprogram\service1\");
            CreateSut();
            stubEnvironment.Received().DownloadFile(Arg.Is<String>(s => s.Equals("http://localhost/TESTAPPLICATION/service1.config/TestMachine")));
        }

        [Test]
        public void verify_correct_url_is_requested_when_environment_variable_is_specified()
        {
            stubEnvironment.GetEnvironmentVariable("").ReturnsForAnyArgs("http://configuration.test.com/baseDirectory/");
            stubEnvironment.DownloadFile("").ReturnsForAnyArgs(a_valid_configuration_file);
            stubEnvironment.GetCurrentPath().Returns(@"c:\testpath\myprogram\myapp\");
            CreateSut();
            stubEnvironment.Received().DownloadFile(Arg.Is<String>(s => s.Equals("http://configuration.test.com/baseDirectory/TESTAPPLICATION/myapp.config/TestMachine")));
        }

        [Test]
        public void verify_support_local_base_address_config_file()
        {

            //Configuration returns standard file 
            stubEnvironment.GetEnvironmentVariable("").ReturnsForAnyArgs("");
            stubEnvironment.DownloadFile("").ReturnsForAnyArgs(a_valid_configuration_file);
            stubEnvironment.GetFileContent(Arg.Is<String>(s => s.EndsWith(ConfigurationServiceClient.BaseAddressConfigFileName)))
                .Returns("http://configuration.test.com/baseDirectory");
            stubEnvironment.DownloadFile("").ReturnsForAnyArgs(a_valid_configuration_file);
            stubEnvironment.GetCurrentPath().Returns(@"c:\testpath\myprogram\myapp\");
            CreateSut();

            stubEnvironment.Received().DownloadFile(Arg.Is<String>(s => s.Equals("http://configuration.test.com/baseDirectory/TESTAPPLICATION/myapp.config/TestMachine")));
        }

        [Test]
        public void verify_exception_if_no_environment_is_configured()
        {
			var currentPath = Path.Combine ("testpath","myprogram","release");
			var expectedPath = Path.Combine (currentPath, ConfigurationServiceClient.BaseAddressConfigFileName);
            try
            {
                stubEnvironment.GetEnvironmentVariable("").ReturnsForAnyArgs("");
                stubEnvironment.DownloadFile("").ReturnsForAnyArgs(a_valid_configuration_file);
				stubEnvironment.GetCurrentPath().Returns(currentPath);
                CreateSut();
                Assert.Fail("Exception is expected");
            }
            catch (ConfigurationErrorsException ex)
            {
                Assert.That(ex.Message, Contains.Substring("CQRS_TEST_CONFIGURATION_MANAGER"));
				Assert.That(ex.Message, Contains.Substring( expectedPath ));
            }
        }

        [Test]
        public void verify_configuration_url_for_developers()
        {
            stubEnvironment.DownloadFile("").ReturnsForAnyArgs(a_valid_configuration_file);
            stubEnvironment.GetCurrentPath().Returns(@"c:\developing\myfolder\TESTAPPLICATION\src\service1\bin\debug");
            CreateSut();
            stubEnvironment.Received().DownloadFile(Arg.Is<String>(s => s.Equals("http://localhost/TESTAPPLICATION/service1.config/TestMachine")));

            stubEnvironment.GetCurrentPath().Returns(@"c:\developing\myfolder\TESTAPPLICATION\src\service2\bin\release");
            CreateSut();
            stubEnvironment.Received().DownloadFile(Arg.Is<String>(s => s.Equals("http://localhost/TESTAPPLICATION/service2.config/TestMachine")));

            stubEnvironment.GetCurrentPath().Returns(@"c:\developing\myfolder\TESTAPPLICATION\src\service3\bin\x86");
            CreateSut();
            stubEnvironment.Received().DownloadFile(Arg.Is<String>(s => s.Equals("http://localhost/TESTAPPLICATION/service3.config/TestMachine")));

            stubEnvironment.GetCurrentPath().Returns(@"c:\developing\myfolder\TESTAPPLICATION\src\service4\bin\anyfolder");
            CreateSut();
            stubEnvironment.Received().DownloadFile(Arg.Is<String>(s => s.Equals("http://localhost/TESTAPPLICATION/service4.config/TestMachine")));
        }

        [Test]
        public void verify_configuration_url_for_web_developers()
        {
            stubEnvironment.DownloadFile("").ReturnsForAnyArgs(a_valid_configuration_file);
            stubEnvironment.GetCurrentPath().Returns(@"c:\developing\myfolder\myprogram\src\myapplication.web");
            CreateSut();
            stubEnvironment.Received().DownloadFile(Arg.Is<String>(s => s.Equals("http://localhost/TESTAPPLICATION/myapplication.web.config/TestMachine")));
        }

        [Test]
        public void verify_basic_configuration_file_parsing()
        {
            stubEnvironment.DownloadFile("").ReturnsForAnyArgs("{ 'setting' : 'value' }");

            var sut = CreateSut();
            var configuration = sut.GetSetting("setting", "");
            Assert.That(configuration, Is.EqualTo("value"));
        }

        [Test]
        public void verify_correct_handling_of_default_value()
        {
            stubEnvironment.DownloadFile("").ReturnsForAnyArgs(a_valid_configuration_file);

            var sut = CreateSut();
            var configuration = sut.GetSetting("setting_not_in_json_file", "defvalue");
            Assert.That(configuration, Is.EqualTo("defvalue"));
        }

        [Test]
        public void verify_suboject_configuration_parsing()
        {
            stubEnvironment.DownloadFile("").ReturnsForAnyArgs(a_valid_configuration_file_with_suboject);
            var sut = CreateSut();
            var connection1 = sut.GetSetting("connections.conn1", "");
            Assert.That(connection1, Is.EqualTo("valueconn1"));
            var connection2 = sut.GetSetting("connections.conn2", "");
            Assert.That(connection2, Is.EqualTo("valueconn2"));

        }

        [Test]
        public void verify_getting_application_name_with_file_name()
        {
            var baseDir = AppDomain.CurrentDomain.SetupInformation.ApplicationBase;
            var previousDir = new DirectoryInfo(baseDir + "/../../");
            stubEnvironment.GetCurrentPath().Returns(baseDir);
            var fileName = new FileInfo(Path.Combine(previousDir.FullName, "testappname.application"));
            if (!File.Exists(fileName.FullName))
            {
                File.WriteAllText(fileName.FullName, "");
            }
            var sut = new StandardEnvironment();
            var applicationName = sut.GetApplicationName();
            Assert.That(applicationName, Is.EqualTo("testappname"));
        }

        [Test]
        public void verify_nested_configuration_parsing()
        {
            stubEnvironment.DownloadFile("").ReturnsForAnyArgs(a_valid_configuration_file_with_nestedsuboject);
            var sut = CreateSut();
            var connection = sut.GetSetting("connections.mongo.conn1", "");
            Assert.That(connection, Is.EqualTo("mongo1"));
            connection = sut.GetSetting("connections.mongo.conn2", "");
            Assert.That(connection, Is.EqualTo("mongo2"));
            connection = sut.GetSetting("connections.sql.conn1", "");
            Assert.That(connection, Is.EqualTo("sql1"));
            connection = sut.GetSetting("connections.sql.conn4", "");
            Assert.That(connection, Is.EqualTo("sql4"));
        }

        [Test]
        public void verify_correct_handling_of_default_value_dotted_settings()
        {
            stubEnvironment.DownloadFile("").ReturnsForAnyArgs(a_valid_configuration_file);

            var sut = CreateSut();
            var configuration = sut.GetSetting("setting_not_in_json_file", "defvalue1");
            Assert.That(configuration, Is.EqualTo("defvalue1"));
            configuration = sut.GetSetting("notexsits.notexists", "defvalue2");
            Assert.That(configuration, Is.EqualTo("defvalue2"));
        }

        [Test]
        public void verify_complex_object_configuration_settings()
        {
            stubEnvironment.DownloadFile("").ReturnsForAnyArgs(complex_object_configuration_file);
            var sut = CreateSut();
            dynamic configuration = sut.GetStructuredSetting("setting");
            Assert.That((String)configuration.propA, Is.EqualTo("valuea"));
            Assert.That((Int32)configuration.propB, Is.EqualTo(42));
        }

        [Test]
        public void verify_complex_object_in_nested_configuration_settings()
        {
            stubEnvironment.DownloadFile("").ReturnsForAnyArgs(complex_object_configuration_file);

            var sut = CreateSut();
            dynamic configuration = sut.GetStructuredSetting("complex.setting");
            Assert.That((String)configuration.propA, Is.EqualTo("valuea"));
            Assert.That((Int32)configuration.propB, Is.EqualTo(42));
        }

        [Test]
        public void verify_array_configuration()
        {
            stubEnvironment.DownloadFile("").ReturnsForAnyArgs(array_configuration_file);

            var sut = CreateSut();
            IEnumerable<dynamic> configuration = sut.WithArraySetting("setting");
            Assert.That(configuration.Count(), Is.EqualTo(3));
            Assert.That((String)configuration.ElementAt(0), Is.EqualTo("a"));
            Assert.That((String)configuration.ElementAt(1), Is.EqualTo("b"));
            Assert.That((String)configuration.ElementAt(2), Is.EqualTo("c"));

        }

        [Test]
        public void verify_array_configuration_with_usage()
        {
            stubEnvironment.DownloadFile("").ReturnsForAnyArgs(array_configuration_file);

            try
            {
                var sut = CreateSut();
                sut.WithArraySetting("complex.settings", s =>
                {
                    var test = (String)s.ElementAt(0).notExistingProperty;
                    Console.WriteLine(test.Length);
                });
                Assert.Fail("ConfigurationErrorsException is expected");
            }
            catch (ConfigurationErrorsException expectedException)
            {
                //we want a log to be generated.
                Assert.That(expectedException.Message, Contains.Substring("complex.settings"));
            }
        }

        [Test]
        public void verify_array_configuration_with_explicit_test()
        {
            stubEnvironment.DownloadFile("").ReturnsForAnyArgs(array_configuration_file);

            try
            {
                var sut = CreateSut();
                sut.WithArraySetting("complex.settings", s =>
                {
                    var test = (String)s.ElementAt(0).notExistingProperty;
                    if (String.IsNullOrEmpty(test)) return "Missing property notExistingProperty";
                    return "";
                });
                Assert.Fail("ConfigurationErrorsException is expected");
            }
            catch (ConfigurationErrorsException expectedException)
            {
                //we want a log to be generated.
                Assert.That(expectedException.Message, Contains.Substring("notExistingProperty"));
            }
        }

        [Test]
        public void verify_array_of_object_configuration()
        {
            stubEnvironment.DownloadFile("").ReturnsForAnyArgs(array_configuration_file);

            var sut = CreateSut();
            IEnumerable<dynamic> configuration = sut.WithArraySetting("complex.settings");
            Assert.That(configuration.Count(), Is.EqualTo(2));
            Assert.That((String)configuration.ElementAt(0).propA, Is.EqualTo("value1"));
            Assert.That((Int32)configuration.ElementAt(0).propB, Is.EqualTo(1));
            Assert.That((String)configuration.ElementAt(1).propA, Is.EqualTo("value2"));
            Assert.That((Int32)configuration.ElementAt(1).propB, Is.EqualTo(2));
        }

        [Test]
        public void verify_support_last_good_configuration()
        {

            //Configuration server does not return anything
            stubEnvironment.DownloadFile("").ReturnsForAnyArgs(String.Empty);

            //Configuration will ask for last good configuration file and I return last configuration
            stubEnvironment.GetFileContent("").ReturnsForAnyArgs(a_valid_configuration_file);

            var sut = CreateSut();

            var configuration = sut.GetSetting("goodSetting");
            Assert.That(configuration, Is.EqualTo("hello setting"));
            stubEnvironment.Received().GetFileContent(Arg.Is<String>(s => s.EndsWith(ConfigurationServiceClient.LastGoodConfigurationFileName)));
            var received = stubEnvironment.ReceivedCalls().ToList();
            Assert.That(currentTestLogger.Logs.Any(l => l.Contains("last good configuration is used")), "verify warning of last good configuration is used");
        }

        [Test]
        public void verify_saving_last_good_configuration()
        {
            stubEnvironment.DownloadFile("").ReturnsForAnyArgs("{ 'Setting' : 'A sample string'}");
            CreateSut();
            stubEnvironment.Received().SaveFile(
                Arg.Is<String>(s => s.EndsWith("lastGoodConfiguration.config", StringComparison.OrdinalIgnoreCase)), 
                Arg.Is<String>(s => s.Equals("{ 'Setting' : 'A sample string'}")),
                Arg.Any<Boolean>());
           
        }

        [Test]
        public void verify_last_good_configuration_file_blocked_does_not_generate_errors()
        {
            stubEnvironment.DownloadFile("").ReturnsForAnyArgs("{ 'Setting' : 'A sample string'}");
            stubEnvironment.When(s => s.SaveFile(Arg.Any<String>(), Arg.Any<String>(), false))
                .Do(cinfo => { throw new IOException(); });
            CreateSut();
        }

        [Test]
        public void can_download_resource_file()
        {
            stubEnvironment.DownloadFile("").ReturnsForAnyArgs("{ 'Setting' : 'A sample string'}");

            stubEnvironment.DownloadFile("http://localhost/TESTAPPLICATION/resources/myapplication/log4net.config/TestMachine").Returns("log4netconfiguration");
            stubEnvironment.GetCurrentPath().Returns(@"c:\testpath\myapplication\");

            var sut = CreateSut();
            var resource = sut.GetResource("log4net.config");
            stubEnvironment.Received().DownloadFile(Arg.Is<String>(s => s.Equals("http://localhost/TESTAPPLICATION/resources/myapplication/log4net.config/TestMachine")));
            Assert.That(resource, Is.EqualTo("log4netconfiguration"));
        }

        [Test]
        public void can_download_resource_file_on_disk()
        {
            stubEnvironment.DownloadFile("").ReturnsForAnyArgs("{ 'Setting' : 'A sample string'}");
            stubEnvironment.DownloadFile("http://localhost/TESTAPPLICATION/resources/myapplication/log4net.config/TestMachine").Returns("log4netconfiguration");
            stubEnvironment.GetCurrentPath().Returns(@"c:\testpath\myapplication\");
            var sut = CreateSut();
            var resource = sut.DownloadResource("log4net.config");
            stubEnvironment.Received().SaveFile(
                Arg.Is<String>(s => s.EndsWith("log4net.config")),
                Arg.Is<String>(s => s.Equals("log4netconfiguration")), false);
            Assert.That(resource, Is.EqualTo(true));
        }

        [Test]
        public void can_monitor_resource_file_on_disk()
        {
            stubEnvironment.DownloadFile("").ReturnsForAnyArgs("{ 'Setting' : 'A sample string'}");
            String configContent = "resource content";
            stubEnvironment.DownloadFile("http://localhost/TESTAPPLICATION/resources/myapplication/log4net.config/TestMachine").Returns(c => configContent);
            stubEnvironment.GetCurrentPath().Returns(@"c:\testpath\myapplication\");
            var sut = CreateSut();
            var changedContent = "";
            var resource = sut.DownloadResource("log4net.config", monitorForChange: true);

            //now force polling
            configContent = "modified content";
            sut.CheckForMonitoredResourceChange();
            stubEnvironment.Received().SaveFile(
               Arg.Is<String>(s => s.EndsWith("log4net.config")),
               Arg.Is<String>(s => s.Equals(configContent)), false);
        }

        [Test]
        public void monitoring_is_disabled_by_default()
        {
            stubEnvironment.DownloadFile("").ReturnsForAnyArgs("{ 'Setting' : 'A sample string'}");
            String configContent = "resource content";
            stubEnvironment.DownloadFile("http://localhost/TESTAPPLICATION/resources/myapplication/log4net.config/TestMachine").Returns(c => configContent);
            stubEnvironment.GetCurrentPath().Returns(@"c:\testpath\myapplication\");
            var sut = CreateSut();
            var changedContent = "";
            var resource = sut.DownloadResource("log4net.config");

            //now force polling
            configContent = "modified content";
            sut.CheckForMonitoredResourceChange();
            stubEnvironment.Received().DidNotReceive().SaveFile(
               Arg.Is<String>(s => s.EndsWith("log4net.config")),
               Arg.Is<String>(s => s.Equals("modified content")), false);
        }

        [Test]
        public void monitored_resources_does_not_change_files_if_content_not_changed()
        {
            stubEnvironment.DownloadFile("").ReturnsForAnyArgs("{ 'Setting' : 'A sample string'}");
            String configContent = "resource content";
            stubEnvironment.DownloadFile("http://localhost/TESTAPPLICATION/resources/myapplication/log4net.config/TestMachine").Returns(c => configContent);
            stubEnvironment.GetCurrentPath().Returns(@"c:\testpath\myapplication\");
            var sut = CreateSut();
            var resource = sut.DownloadResource("log4net.config", monitorForChange : true);

            //now force polling
            sut.CheckForMonitoredResourceChange();
            stubEnvironment.Received().SaveFile(
               Arg.Is<String>(s => s.EndsWith("log4net.config")),
               Arg.Is<String>(s => s.Equals(configContent)), false);
        }

        private TestLogger currentTestLogger;

        private ConfigurationServiceClient CreateSut()
        {
            currentTestLogger = new TestLogger();
            return new ConfigurationServiceClient(currentTestLogger.Log, "CQRS_TEST_CONFIGURATION_MANAGER", stubEnvironment);
        }

       
    }
}
