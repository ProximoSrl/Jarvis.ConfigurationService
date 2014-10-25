using Jarvis.ConfigurationService.Client.Support;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Jarvis.ConfigurationService.Client
{
    public class ConfigurationServiceClient
    {
        public static ConfigurationServiceClient Instance
        {
            get
            {
                if (_instance == null) throw new ConfigurationErrorsException("Cqrs Configuration Manager not initialized, you need to initialize it with Castle Installer (CqrsConfigurationInstaller).");
                return _instance;
            }
        }

        internal const String BaseAddressConfigFileName = "baseConfigAddress.config";
        internal const String LastGoodConfigurationFileName = "lastGoodConfiguration.config";

        private Action<String, Boolean, Exception> _logger;
        private static ConfigurationServiceClient _instance;
        private JObject _configurationObject;
        private String _configFileLocation;
        private String _baseServerAddressEnvironmentVariable;

        private IEnvironment _environment;

        internal Action<String, Boolean, Exception> Logger
        {
            get { return _logger ?? DebugDefaultLogger; }
            set { _logger = value; }
        }

        public string ConfigFileLocation
        {
            get { return _configFileLocation; }
        }

        internal Action<String, Boolean, Exception> DebugDefaultLogger = (message, isError, exception) => Debug.WriteLine("Log: " + message + " Iserror: " + isError);

        public static void AppDomainInitializer
             (
                Action<String, Boolean, Exception> loggerFunction,
                String baseServerAddressEnvironmentVariable
            )
        {
            _instance = new ConfigurationServiceClient(loggerFunction, baseServerAddressEnvironmentVariable, new StandardEnvironment());
        }


        internal ConfigurationServiceClient
            (
                Action<String, Boolean, Exception> loggerFunction,
                String baseServerAddressEnvironmentVariable,
                IEnvironment environment
            )
        {
            _logger = loggerFunction;
            _baseServerAddressEnvironmentVariable = baseServerAddressEnvironmentVariable;
            _environment = environment;
            AutoConfigure();
            LoadSettings();
        }

        void LoadSettings()
        {
            String configurationFullContent;
            try
            {
                configurationFullContent = _environment.DownloadFile(ConfigFileLocation);
            }
            catch (ConfigurationErrorsException ex)
            {
                throw new ConfigurationErrorsException(ex.Message);
            }

            //If server did not responded we can use last good configuration
            if (String.IsNullOrEmpty(configurationFullContent))
            {
                configurationFullContent = _environment.GetFileContent(Path.Combine(_environment.GetCurrentPath(), LastGoodConfigurationFileName));
                if (!String.IsNullOrEmpty(configurationFullContent))
                {
                    LogDebug("Configuration server " + ConfigFileLocation + "did not responded, last good configuration is used");
                }
            }
            if (String.IsNullOrEmpty(configurationFullContent))
            {
                String errorString = "Cannot Find configuration value at url: " + ConfigFileLocation;
                throw new ConfigurationErrorsException(errorString);
            }
            try
            {
                LogDebug(String.Format("Configuration is:\n{0}\n-------------", configurationFullContent));
                _configurationObject = (JObject) JsonConvert.DeserializeObject(configurationFullContent);
                if (_configurationObject == null)
                {
                    throw new Exception("Configuration is null");
                }
                _environment.SaveFile(Path.Combine(_environment.GetCurrentPath(), LastGoodConfigurationFileName), configurationFullContent);
            }
            catch (Exception ex)
            {
                var errorString = String.Format("Malformed json configuration: {0}\n{1}",
                    ex.Message,
                    configurationFullContent
                    );
                LogError(errorString, ex);
                throw new ConfigurationErrorsException(errorString);
            }
        }

        void AutoConfigure()
        {
            var currentPath = _environment.GetCurrentPath();
            var splittedPath = currentPath
                .TrimEnd('/', '\\')
                .Split('/', '\\');
            var moduleName = splittedPath[splittedPath.Length - 1];
            var applicationName = _environment.GetApplicationName();
            //if a bin folder is present we are in "developer mode" module name is name of the folder before /bin/
            //and not the previous folder
            if (splittedPath.Contains("bin"))
            {
                moduleName = splittedPath.Reverse().SkipWhile(s => s != "bin").Skip(1).FirstOrDefault();
                if (String.IsNullOrEmpty(applicationName))
                {
                    String errorString =
                        @"Unable to determine application name. Since bin is present in the path of the exe I'm expecting the path to be in the form of: x:\xxxx\APPLICATIONAME\SRC\..\..\BIN\XXX.EXE";
                    throw new ConfigurationErrorsException(errorString);
                }
            }
            //Try grab base address from a local config file then from env variable

            var baseConfigurationServer = _environment.GetFileContent(Path.Combine(_environment.GetCurrentPath(), BaseAddressConfigFileName));
            if (String.IsNullOrEmpty(baseConfigurationServer))
            {
                baseConfigurationServer = _environment.GetEnvironmentVariable(_baseServerAddressEnvironmentVariable);
            }
            if (String.IsNullOrEmpty(baseConfigurationServer))
            {
                String errorString =
                    string.Format(
                        "You need to specify base address for configuration server in environment variable: {0} or in config file {1}",
                        _baseServerAddressEnvironmentVariable,
                        Path.Combine(_environment.GetCurrentPath(), BaseAddressConfigFileName)
                        );
                throw new ConfigurationErrorsException(errorString);
            }
            _configFileLocation = String.Format(
                "{0}/{1}/{2}.config/{3}",
                baseConfigurationServer.TrimEnd('/', '\\'),
                applicationName,
                moduleName,
                _environment.GetMachineName());

            LogDebug("Loading configuration from " + _configFileLocation);
        }

        void LogError(string message, Exception ex)
        {
            Logger(message, true, ex);
        }

        void LogDebug(string message)
        {
            Logger(message, false, null);
        }

        public void WithSetting(string settingName, string settingDefaultValue, Action<String> useConfigurationFunction)
        {
            var setting = GetSetting(settingName, settingDefaultValue);
            InnerUseConfigurationFunction(s =>
            {
                useConfigurationFunction(s);
                return "";
            }, setting, settingName);
        }

        public void WithSetting(string settingName, Action<String> useConfigurationFunction)
        {
            var setting = GetSetting(settingName);
            InnerUseConfigurationFunction(s =>
            {
                useConfigurationFunction(s);
                return "";
            }, setting, settingName);
        }

        public void WithSetting(string settingName, string settingDefaultValue, Func<String, String> useConfigurationFunction)
        {
            var setting = GetSetting(settingName, settingDefaultValue);
            InnerUseConfigurationFunction(useConfigurationFunction, setting, settingName);
        }

        public void WithSetting(string settingName, Func<String, String> useConfigurationFunction)
        {
            var setting = GetSetting(settingName);
            InnerUseConfigurationFunction(useConfigurationFunction, setting, settingName);
        }

        public String GetSetting(string settingName, string settingDefaultValue)
        {
            return InternalGetSetting(settingName) ?? settingDefaultValue;
        }

        public String GetSetting(string settingName)
        {
            var setting = InternalGetSetting(settingName);

            if (setting == null)
            {
                String errorString = "Required setting '" + settingName + "' not found in configuration: " + ConfigFileLocation;
                LogError(errorString, null);
                throw new ConfigurationErrorsException(errorString);
            }

            return setting;
        }

        private String InternalGetSetting(string settingName)
        {
            var path = settingName.Split('.');
            JObject current = _configurationObject;
            for (int i = 0; i < path.Length - 1; i++)
            {
                if (current[path[i]] == null) return null;
                current = (JObject)current[path[i]];
            }
            if (current[path.Last()] == null)
                return null;
            return current[path.Last()].ToString();
        }

        private void InnerUseConfigurationFunction<T>(Func<T, String> useConfigurationFunction, T setting, String settingName)
        {
            String error;
            try
            {
                error = useConfigurationFunction(setting);
            }
            catch (Exception ex)
            {
                String errorString = "Error during usage of configuration '" + settingName + "' - Config location: " + ConfigFileLocation + " - error: " + ex.Message;
                LogError(errorString, ex);
                throw new ConfigurationErrorsException(errorString);
            }
            if (!String.IsNullOrEmpty(error))
            {
                String errorString = "Error during usage of configuration '" + settingName + "' - Config location: " + ConfigFileLocation + " - error: " + error;
                LogError(errorString, null);
                throw new ConfigurationErrorsException(errorString);
            }
        }

        #region Complex parameter handling

        /// <summary>
        /// Returns complex object configuration, since configuration is json based
        /// this is valid when some configuration has sub object as settings.
        /// </summary>
        /// <param name="settingName"></param>
        /// <returns></returns>
        public dynamic GetStructuredSetting(string settingName)
        {
            var setting = InternalGetSetting(settingName);
            return (dynamic)JsonConvert.DeserializeObject(setting);
        }

        /// <summary>
        /// If the configuration is an array of object, it is good to 
        /// have an option to get the configuration as array. The return
        /// value is an enumeration of dynamic object because you can 
        /// also return array of complex objects.
        /// </summary>
        /// <param name="settingName"></param>
        /// <returns></returns>
        public IEnumerable<dynamic> WithArraySetting(string settingName)
        {
            var setting = InternalGetSetting(settingName);
            return (JArray)JsonConvert.DeserializeObject(setting);
        }

        public void WithArraySetting(string settingName, Func<IEnumerable<dynamic>, String> useConfigurationFunction)
        {
            var setting = WithArraySetting(settingName);
            InnerUseConfigurationFunction(useConfigurationFunction, setting, settingName);
        }

        public void WithArraySetting(string settingName, Action<IEnumerable<dynamic>> useConfigurationFunction)
        {
            var setting = WithArraySetting(settingName);
            InnerUseConfigurationFunction(s =>
            {
                useConfigurationFunction(s);
                return "";
            }, setting, settingName);
        }

        #endregion

       
    }
}
