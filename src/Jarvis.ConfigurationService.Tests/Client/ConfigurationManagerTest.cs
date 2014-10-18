using System.Collections;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using Jarvis.ConfigurationService.Client;

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

        [SetUp]
        public void SetUp()
        {
            //valid directory as base test execution path.
            ConfigurationServiceClient.GetCurrentPath = () => @"c:\develop\blabla\nameofsoftware\src\blabla\bin\debug";
            ConfigurationServiceClient.DownloadFile = s => String.Empty;
            ConfigurationServiceClient.GetFileContent = fileName => String.Empty; //base situation, no last good configuration.
            ConfigurationServiceClient.GetEnvironmentVariable = variableName => "http://localhost";
            ConfigurationServiceClient.SaveFile = (fn, cnt) => Console.WriteLine("Saving " + fn);
            ConfigurationServiceClient.GetApplicationName = () => "TESTAPPLICATION";
        }

        [Test]
        public void should_throw_exception_if_no_configuration_file_is_provided()
        {
            ConfigurationServiceClient.DownloadFile = s => String.Empty;

            Assert.Throws<ConfigurationErrorsException>(() => CreateSut());
        }

        [Test]
        public void exception_for_no_configuration_file_found_should_tell_where_the_file_is_expected()
        {
            ConfigurationServiceClient.DownloadFile = s => String.Empty;
            ConfigurationServiceClient.GetCurrentPath = () => @"c:\testpath\myprogram\myapp\";
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
            ConfigurationServiceClient.DownloadFile = s => an_invalid_configuration_file;
            ConfigurationServiceClient.GetCurrentPath = () => @"c:\testpath\myprogram\release\";
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
        public void exception_if_settings_not_present_and_no_default_value_specified()
        {
            ConfigurationServiceClient.DownloadFile = s => a_valid_configuration_file;
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
            ConfigurationServiceClient.DownloadFile = s => a_valid_configuration_file;
            ConfigurationServiceClient.GetCurrentPath = () => @"c:\testpath\myprogram\myapp\";
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
            ConfigurationServiceClient.DownloadFile = s => a_valid_configuration_file;
            ConfigurationServiceClient.GetCurrentPath = () => @"c:\testpath\myprogram\release\";

            var sut = CreateSut();
            sut.WithSetting("goodSetting", (setting) =>
            {
                //Doing something, and if something is wrong just raise an exception
            });
        }

        [Test]
        public void configuration_manager_is_able_to_accept_testing_configuration()
        {
            ConfigurationServiceClient.DownloadFile = s => a_valid_configuration_file;
            ConfigurationServiceClient.GetCurrentPath = () => @"c:\testpath\myprogram\myapp\";
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
            ConfigurationServiceClient.DownloadFile = s => a_valid_configuration_file;
            ConfigurationServiceClient.GetCurrentPath = () => @"c:\testpath\myprogram\myapp\";
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
            ConfigurationServiceClient.DownloadFile = s => a_valid_configuration_file;
            ConfigurationServiceClient.GetCurrentPath = () => @"c:\testpath\myprogram\myapp\";
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
            ConfigurationServiceClient.DownloadFile = s => a_valid_configuration_file;
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
            String pathCalled = "";
            ConfigurationServiceClient.DownloadFile = s =>
            {
                pathCalled = s;
                return a_valid_configuration_file;
            };
            ConfigurationServiceClient.GetCurrentPath = () => @"c:\testpath\myprogram\service1\";
            CreateSut();

            Assert.That(pathCalled, Is.EqualTo("http://localhost/TESTAPPLICATION/service1.config"));
        }

        [Test]
        public void verify_correct_url_is_requested_when_environment_variable_is_specified()
        {
            String pathCalled = "";
            ConfigurationServiceClient.GetEnvironmentVariable = v => "http://configuration.test.com/baseDirectory/";
            ConfigurationServiceClient.DownloadFile = s =>
            {
                pathCalled = s;
                return a_valid_configuration_file;
            };
            ConfigurationServiceClient.GetCurrentPath = () => @"c:\testpath\myprogram\myapp\";
            CreateSut();

            Assert.That(pathCalled, Is.EqualTo("http://configuration.test.com/baseDirectory/TESTAPPLICATION/myapp.config"));
        }

        [Test]
        public void verify_support_local_base_address_config_file()
        {

            //Configuration returns standard file 
            ConfigurationServiceClient.DownloadFile = s => a_valid_configuration_file;

            //we have no environment variable
            ConfigurationServiceClient.GetEnvironmentVariable = variableName => String.Empty;
            //but we have local configuration file to specify base address.
            ConfigurationServiceClient.GetFileContent = fileName =>
            {
                if (fileName.EndsWith(ConfigurationServiceClient.baseAddressConfigFileName))
                {
                    return "http://configuration.test.com/baseDirectory";
                }
                return "";
            };
            String pathCalled = "";
            ConfigurationServiceClient.DownloadFile = s =>
            {
                pathCalled = s;
                return a_valid_configuration_file;
            };
            ConfigurationServiceClient.GetCurrentPath = () => @"c:\testpath\myprogram\myapp\";
            CreateSut();

            Assert.That(pathCalled, Is.EqualTo("http://configuration.test.com/baseDirectory/TESTAPPLICATION/myapp.config"));
        }

        [Test]
        public void verify_exception_if_no_environment_is_configured()
        {
            String pathCalled = "";
            ConfigurationServiceClient.GetEnvironmentVariable = v => "";
            try
            {
                ConfigurationServiceClient.DownloadFile = s =>
                {
                    pathCalled = s;
                    return a_valid_configuration_file;
                };
                ConfigurationServiceClient.GetCurrentPath = () => @"c:\testpath\myprogram\release\";
                CreateSut();
                Assert.Fail("Exception is expected");
            }
            catch (ConfigurationErrorsException ex)
            {
                Assert.That(ex.Message, Contains.Substring("CQRS_TEST_CONFIGURATION_MANAGER"));
                Assert.That(ex.Message, Contains.Substring(@"c:\testpath\myprogram\release\" + ConfigurationServiceClient.baseAddressConfigFileName));
            }
        }

        [Test]
        public void verify_configuration_url_for_developers()
        {
            String pathCalled = "";
            ConfigurationServiceClient.DownloadFile = s =>
            {
                pathCalled = s;
                return a_valid_configuration_file;
            };
            ConfigurationServiceClient.GetCurrentPath = () => @"c:\developing\myfolder\TESTAPPLICATION\src\service1\bin\debug";
            CreateSut();
            Assert.That(pathCalled, Is.EqualTo("http://localhost/TESTAPPLICATION/service1.config"));

            ConfigurationServiceClient.GetCurrentPath = () => @"c:\developing\myfolder\TESTAPPLICATION\src\service2\bin\release";
            CreateSut();
            Assert.That(pathCalled, Is.EqualTo("http://localhost/TESTAPPLICATION/service2.config"));

            ConfigurationServiceClient.GetCurrentPath = () => @"c:\developing\myfolder\TESTAPPLICATION\src\service3\bin\x86";
            CreateSut();
            Assert.That(pathCalled, Is.EqualTo("http://localhost/TESTAPPLICATION/service3.config"));

            ConfigurationServiceClient.GetCurrentPath = () => @"c:\developing\myfolder\TESTAPPLICATION\src\service4\bin\anyfolder";
            CreateSut();

            Assert.That(pathCalled, Is.EqualTo("http://localhost/TESTAPPLICATION/service4.config"));
        }

        [Test]
        public void verify_configuration_url_for_web_developers()
        {
            String pathCalled = "";
            ConfigurationServiceClient.DownloadFile = s =>
            {
                pathCalled = s;
                return a_valid_configuration_file;
            };
            ConfigurationServiceClient.GetCurrentPath = () => @"c:\developing\myfolder\myprogram\src\myapplication.web";
            CreateSut();

            Assert.That(pathCalled, Is.EqualTo("http://localhost/TESTAPPLICATION/myapplication.web.config"));


        }

        [Test]
        public void verify_basic_configuration_file_parsing()
        {
            ConfigurationServiceClient.DownloadFile = s => "{ 'setting' : 'value' }";

            var sut = CreateSut();
            var configuration = sut.GetSetting("setting", "");
            Assert.That(configuration, Is.EqualTo("value"));
        }

        [Test]
        public void verify_correct_handling_of_default_value()
        {
            ConfigurationServiceClient.DownloadFile = s => a_valid_configuration_file;

            var sut = CreateSut();
            var configuration = sut.GetSetting("setting_not_in_json_file", "defvalue");
            Assert.That(configuration, Is.EqualTo("defvalue"));
        }

        [Test]
        public void verify_suboject_configuration_parsing()
        {
            ConfigurationServiceClient.DownloadFile = s => a_valid_configuration_file_with_suboject;

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
            ConfigurationServiceClient.GetCurrentPath = () => baseDir;
            var fileName = new FileInfo(Path.Combine(previousDir.FullName, "testappname.application"));
            if (!File.Exists(fileName.FullName))
            {
                File.WriteAllText(fileName.FullName, "");
            }

            var applicationName = ConfigurationServiceClient._GetApplicationName();
            Assert.That(applicationName, Is.EqualTo("testappname"));
        }

        [Test]
        public void verify_nested_configuration_parsing()
        {
            ConfigurationServiceClient.DownloadFile = s => a_valid_configuration_file_with_nestedsuboject;

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
            ConfigurationServiceClient.DownloadFile = s => a_valid_configuration_file;

            var sut = CreateSut();
            var configuration = sut.GetSetting("setting_not_in_json_file", "defvalue1");
            Assert.That(configuration, Is.EqualTo("defvalue1"));
            configuration = sut.GetSetting("notexsits.notexists", "defvalue2");
            Assert.That(configuration, Is.EqualTo("defvalue2"));
        }

        [Test]
        public void verify_complex_object_configuration_settings()
        {
            ConfigurationServiceClient.DownloadFile = s => complex_object_configuration_file;

            var sut = CreateSut();
            dynamic configuration = sut.GetStructuredSetting("setting");
            Assert.That((String)configuration.propA, Is.EqualTo("valuea"));
            Assert.That((Int32)configuration.propB, Is.EqualTo(42));
        }

        [Test]
        public void verify_complex_object_in_nested_configuration_settings()
        {
            ConfigurationServiceClient.DownloadFile = s => complex_object_configuration_file;

            var sut = CreateSut();
            dynamic configuration = sut.GetStructuredSetting("complex.setting");
            Assert.That((String)configuration.propA, Is.EqualTo("valuea"));
            Assert.That((Int32)configuration.propB, Is.EqualTo(42));
        }

        [Test]
        public void verify_array_configuration()
        {
            ConfigurationServiceClient.DownloadFile = s => array_configuration_file;

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
            ConfigurationServiceClient.DownloadFile = s => array_configuration_file;

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
            ConfigurationServiceClient.DownloadFile = s => array_configuration_file;

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
            ConfigurationServiceClient.DownloadFile = s => array_configuration_file;

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
            ConfigurationServiceClient.DownloadFile = s => String.Empty;

            //Configuration will ask for last good configuration file and I return last configuration
            String calledFileName = "";
            ConfigurationServiceClient.GetFileContent = fileName =>
            {
                calledFileName = fileName;
                return a_valid_configuration_file;
            };

            var sut = CreateSut();

            var configuration = sut.GetSetting("goodSetting");
            Assert.That(configuration, Is.EqualTo("hello setting"));
            Assert.That(calledFileName, Is.StringEnding(ConfigurationServiceClient.lastGoodConfigurationFileName));
            Assert.That(currentTestLogger.Logs.Any(l => l.Contains("last good configuration is used")), "verify warning of last good configuration is used");
        }

        [Test]
        public void verify_saving_last_good_configuration()
        {
            //Configuration server does not return anything
            ConfigurationServiceClient.DownloadFile = s => "{ 'Setting' : 'A sample string'}";
            String contentFile = "";
            ConfigurationServiceClient.SaveFile = (fn, content) => contentFile = content;
            var sut = CreateSut();

            Assert.That(contentFile, Is.EqualTo("{ 'Setting' : 'A sample string'}"));
        }

        private TestLogger currentTestLogger;

        private ConfigurationServiceClient CreateSut()
        {
            currentTestLogger = new TestLogger();
            return new ConfigurationServiceClient(currentTestLogger.Log, "CQRS_TEST_CONFIGURATION_MANAGER");
        }

        private class TestLogger 
        {
            public Int32 ErrorCount { get; set; }

            public List<String> Logs { get; set; }

            public TestLogger()
            {
                Logs = new List<string>();
            }
            public void Log(String message, Boolean isError, Exception ex) 
            {
                if (isError) ErrorCount++;
                Logs.Add(message);
            }
        }
    }
}
