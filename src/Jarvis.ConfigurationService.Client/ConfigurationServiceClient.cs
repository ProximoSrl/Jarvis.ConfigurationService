﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
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

        internal const String baseAddressConfigFileName = "baseConfigAddress.config";
        internal const String lastGoodConfigurationFileName = "lastGoodConfiguration.config";

        private ILogger _logger;
        private static ConfigurationServiceClient _instance;
        private JObject _configurationObject;
        private String _configFileLocation;

        private String _baseServerAddressEnvironmentVariable;

        internal ILogger Logger
        {
            get { return _logger ?? NullLogger.Instance; }
            set { _logger = value; }
        }

        internal ConfigurationServiceClient
            (
                ILogger logger,
                String baseServerAddressEnvironmentVariable
            )
        {
            _logger = logger;
            _baseServerAddressEnvironmentVariable = baseServerAddressEnvironmentVariable;
            var currentPath = GetCurrentPath();
            var splittedPath = currentPath
                .TrimEnd('/', '\\')
                .Split('/', '\\');
            var moduleName = splittedPath[splittedPath.Length - 1];
            var applicationName = GetApplicationName();
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

            var baseConfigurationServer = GetFileContent(Path.Combine(GetCurrentPath(), baseAddressConfigFileName));
            if (String.IsNullOrEmpty(baseConfigurationServer))
            {
                baseConfigurationServer = GetEnvironmentVariable(_baseServerAddressEnvironmentVariable);
            }
            if (String.IsNullOrEmpty(baseConfigurationServer))
            {
                String errorString =
                    string.Format(
                        "You need to specify base address for configuration server in environment variable: {0} or in config file {1}",
                        _baseServerAddressEnvironmentVariable,
                        Path.Combine(GetCurrentPath(), baseAddressConfigFileName)
                    );
                throw new ConfigurationErrorsException(errorString);
            }
            _configFileLocation = String.Format("{0}/{1}/{2}/config.json", baseConfigurationServer.TrimEnd('/', '\\'), applicationName, moduleName);
            var configurationFullContent = DownloadFile(_configFileLocation);
            //If server did not responded we can use last good configuration
            if (String.IsNullOrEmpty(configurationFullContent))
            {
                configurationFullContent = GetFileContent(Path.Combine(GetCurrentPath(), lastGoodConfigurationFileName));
                if (!String.IsNullOrEmpty(configurationFullContent))
                {
                    Logger.Warn("Configuration server " + _configFileLocation + "did not responded, last good configuration is used");
                }
            }
            if (String.IsNullOrEmpty(configurationFullContent))
            {
                String errorString = "Cannot Find configuration value at url: " + _configFileLocation;
                throw new ConfigurationErrorsException(errorString);
            }
            try
            {
                _configurationObject = (JObject)JsonConvert.DeserializeObject(configurationFullContent);
                SaveFile(Path.Combine(GetCurrentPath(), lastGoodConfigurationFileName), configurationFullContent);
            }
            catch (Exception ex)
            {
                String errorString = "Malformed json configuration file: " + ex.Message;
                Logger.Error(errorString);
                throw new ConfigurationErrorsException(errorString);
            }
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
                String errorString = "Required setting '" + settingName + "' not found in configuration: " + _configFileLocation;
                Logger.Error(errorString);
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
                String errorString = "Error during usage of configuration '" + settingName + "' - Config location: " + _configFileLocation + " - error: " + ex.Message;
                Logger.Error(errorString, ex);
                throw new ConfigurationErrorsException(errorString);
            }
            if (!String.IsNullOrEmpty(error))
            {
                String errorString = "Error during usage of configuration '" + settingName + "' - Config location: " + _configFileLocation + " - error: " + error;
                Logger.Error(errorString);
                throw new ConfigurationErrorsException(errorString);
            }
        }


        #region Manager class


        #endregion

        #region Access To Environment

        /// <summary>
        /// Used to access a file on the network or to call a service that answer with
        /// a full json file.
        /// </summary>
        internal static Func<String, String> DownloadFile = address =>
        {
            try
            {
                using (var wc = new WebClient())
                {
                    wc.Headers.Add(HttpRequestHeader.ContentType, "application/json");
                    return wc.DownloadString(address);
                }
            }
            catch (Exception ex)
            {
                return String.Empty;
            }
        };

        internal static Func<String, String> GetFileContent = fileName =>
        {
            if (!File.Exists(fileName))
            {
                return null;
            }
            return File.ReadAllText(fileName);
        };

        internal static Func<String> GetApplicationName = _GetApplicationName;

        internal static String _GetApplicationName()
        {
            var actualFolder = GetCurrentPath();
            DirectoryInfo currentDirectory = new DirectoryInfo(actualFolder);
            do
            {
                var applicationFile = currentDirectory
                    .GetFiles("*.application")
                    .FirstOrDefault();
                if (applicationFile != null)
                {
                    return Path.GetFileNameWithoutExtension(applicationFile.FullName);
                }
                currentDirectory = currentDirectory.Parent;
            } while (currentDirectory != null);
            throw new ConfigurationErrorsException(
                "Unable to find file with extension .application to find application name");
        }

        internal static Action<String, String> SaveFile = File.WriteAllText;

        /// <summary>
        /// Used to access current path of the application
        /// </summary>
        internal static Func<String> GetCurrentPath = () => AppDomain.CurrentDomain.SetupInformation.ApplicationBase;

        /// <summary>
        /// Access environment variable.
        /// </summary>
        internal static Func<String, String> GetEnvironmentVariable = Environment.GetEnvironmentVariable;

        #endregion

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

        public class CqrsConfigurationInstaller : IWindsorInstaller
        {
            private String _baseServerAddressEnvironmentVariable;
            public CqrsConfigurationInstaller
                (
                    String baseServerAddressEnvironmentVariable
                )
            {
                _baseServerAddressEnvironmentVariable = baseServerAddressEnvironmentVariable;
            }

            public void Install(Castle.Windsor.IWindsorContainer container, Castle.MicroKernel.SubSystems.Configuration.IConfigurationStore store)
            {
                ILoggerFactory factory = container.Resolve<ILoggerFactory>();
                var logger = factory.Create(typeof(CqrsConfigurationManager));
                try
                {
                    CqrsConfigurationManager._instance = new CqrsConfigurationManager(logger, _baseServerAddressEnvironmentVariable);
                }
                catch (ConfigurationErrorsException ex)
                {
                    logger.Error(ex.Message, ex);
                    throw;
                }
            }
        }
    }
}
