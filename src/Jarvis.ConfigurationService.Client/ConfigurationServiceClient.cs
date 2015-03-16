using Jarvis.ConfigurationService.Client.Support;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

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

        internal static IDisposable OverrideInstanceForUnitTesting(ConfigurationServiceClient overrideInstance) 
        {
            var actual = _instance;
            _instance = overrideInstance;
            return new DisposableAction(() => _instance = actual);
        }

        internal const String LastGoodConfigurationFileName = "lastGoodConfiguration.config";

        private Action<String, Boolean, Exception> _logger;
        private static ConfigurationServiceClient _instance;
        private JObject _configurationObject;
        private String _configFileLocation;
        private String _baseServerAddressEnvironmentVariable;
        private String _baseConfigurationServer;
        private String _moduleName;
        private ClientConfiguration _clientConfiguration;

        private IEnvironment _environment;

        private Timer _configChangePollerTimer;

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
            _resourceToMonitor = new ConcurrentDictionary<String, MonitoredFile>();
            AutoConfigure();
            LoadSettings();
            _configChangePollerTimer = new Timer(60 * 1000);
            _configChangePollerTimer.Elapsed += PollServerForChangeInConfiguration;
        }

        void LoadSettings()
        {
            String configurationFullContent = null;
            try
            {
                configurationFullContent = _environment.DownloadFile(ConfigFileLocation);
            }
            catch (ServerConfigurationException ex)
            {
                LogError("Server configuration exception.", ex);
                if (ex.ServerResponse.Contains("Error reading config file"))
                    throw new ConfigurationErrorsException("Server Configuration Exception", ex);
            }
            catch (Exception ex)
            {
                LogError("Unable to connect to remote server", ex);
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
                _environment.SaveFile(Path.Combine(_environment.GetCurrentPath(), LastGoodConfigurationFileName), configurationFullContent, true);
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
            _moduleName = splittedPath[splittedPath.Length - 1];
            _clientConfiguration = _environment.GetApplicationConfig();
            //if a bin folder is present we are in "developer mode" module name is name of the folder before /bin/
            //and not the previous folder
            if (splittedPath.Contains("bin"))
            {
                _moduleName = splittedPath.Reverse().SkipWhile(s => s != "bin").Skip(1).FirstOrDefault();
                if (_clientConfiguration == null)
                {
                    String errorString =
                        @"Unable to determine application name. Since bin is present in the path of the exe I'm expecting the path to be in the form of: x:\xxxx\APPLICATIONAME\SRC\..\..\BIN\XXX.EXE";
                    throw new ConfigurationErrorsException(errorString);
                }
            }
            //Try grab base address from a local config file then from env variable

            _baseConfigurationServer = _clientConfiguration.BaseServerAddress;
            if (String.IsNullOrEmpty(_baseConfigurationServer))
            {
                _baseConfigurationServer = _environment.GetEnvironmentVariable(_baseServerAddressEnvironmentVariable);
            }
            if (String.IsNullOrEmpty(_baseConfigurationServer))
            {
                String errorString =
                    string.Format(
                        "You need to specify base address for configuration server in environment variable: {0} or in .application file with setting base-server-address",
                        _baseServerAddressEnvironmentVariable
                        );
                throw new ConfigurationErrorsException(errorString);
            }
            _configFileLocation = String.Format(
                "{0}/{1}/{2}.config/{3}",
                _baseConfigurationServer.TrimEnd('/', '\\'),
                _clientConfiguration.ApplicationName,
                _moduleName,
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

        #region Resource Handling

        private ConcurrentDictionary<String, MonitoredFile> _resourceToMonitor;

        private class MonitoredFile 
        {
            public String Content { get; set; }

            public String LocalFileName { get; set; }

            public Action<String> Callback { get; set; }
        }

        internal void PollServerForChangeInConfiguration(object sender, ElapsedEventArgs e)
        {
            CheckForMonitoredResourceChange();
        }

        public void CheckForMonitoredResourceChange() 
        {
            if (_resourceToMonitor.Count == 0) return;
            foreach (var res in _resourceToMonitor)
            {
                var resourceValue = GetResource(res.Key);
                if (!String.IsNullOrEmpty(resourceValue)) 
                {
                    if (!resourceValue.Equals(res.Value.Content)) 
                    {
                        //configuration is changed, we need to resave the file.
                        res.Value.Content = resourceValue;
                        _environment.SaveFile(res.Value.LocalFileName, resourceValue, false);
                    }
                }
            }
        }

        internal String GetResource(string resourceName)
        {
            var resourceUri = String.Format(
                "{0}/{1}/resources/{2}/{3}/{4}",
                _baseConfigurationServer.TrimEnd('/', '\\'),
                _clientConfiguration.ApplicationName,
                _moduleName,
                resourceName,
                _environment.GetMachineName());
            return _environment.DownloadFile(resourceUri);
        }

        /// <summary>
        /// This function download a resource string, it also copy content on disk on a 
        /// local copy of the resource file. If the configuration service is down or it 
        /// does not answer correctly the client does not touch file. This imply that if
        /// the client was able to download the file the first time, if the configuration
        /// service went down the local version still remain the same.
        /// </summary>
        /// <param name="resourceName">Name of the resource you want to download</param>
        /// <param name="localResourceFileName">Name of the local file, it can be omitted and the 
        /// client will use the same value of <paramref name="resourceName"/></param>
        /// <param name="monitorForChange">true if you want configuration client to poll configuration service
        /// for change and update local file accordingly.</param>
        /// <returns>true if the configuration service respond correctly, false otherwise.</returns>
        public Boolean DownloadResource(
            string resourceName, 
            String localResourceFileName = null, 
            Boolean monitorForChange = false)
        {
            String valueOfFile = GetResource(resourceName);
            if (String.IsNullOrEmpty(valueOfFile)) 
            {
                this.LogError("Configuration server return null content for resource " + resourceName, null);
                return false;
            }
            var savedFileName = Path.Combine(_environment.GetCurrentPath(), localResourceFileName ?? resourceName);
            _environment.SaveFile(savedFileName, valueOfFile, false);
            if (monitorForChange) 
            {
                var monitoredFile = new MonitoredFile()
                {
                    Content = valueOfFile,
                    LocalFileName = savedFileName,
                };
                _resourceToMonitor.AddOrUpdate(resourceName, monitoredFile, (r, h) => monitoredFile);
            }
            return true;
        }


        #endregion



      
    }
}
