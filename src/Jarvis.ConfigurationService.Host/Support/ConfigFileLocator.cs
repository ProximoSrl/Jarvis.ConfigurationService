using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Configuration;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using log4net.Repository.Hierarchy;
using Newtonsoft.Json;
using log4net;
using Jarvis.ConfigurationService.Host.Model;

namespace Jarvis.ConfigurationService.Host.Support
{
    internal class ComposedConfiguration
    {
        public Object Configuration { get; set; }

        public ParameterManager.ReplaceResult ReplaceResult { get; set; }

        public ComposedConfiguration(
            Object configuration,
            ParameterManager.ReplaceResult replaceResult)
        {
            Configuration = configuration;
            ReplaceResult = replaceResult;
        }
    }

    /// <summary>
    /// Class used to phisically locate and compose json files.
    /// </summary>
    public static class ConfigFileLocator
    {
        private static ILog _logger = LogManager.GetLogger(typeof(ConfigFileLocator));

        /// <summary>
        /// 
        /// </summary>
        /// <param name="baseDirectory"></param>
        /// <param name="applicationName"></param>
        /// <param name="serviceName"></param>
        /// <param name="hostName"></param>
        /// <param name="defaultConfiguration">Json configuration sent by the client
        /// that represents base configuration for the application. It has lower priority. It can be null.</param>
        /// <param name="defaultParameters">Json configuration sent by the client
        /// to contain base parameters for the application, it has lower priority. It can be null.</param>
        /// <returns></returns>
        internal static ComposedConfiguration GetConfig
            (
                String baseDirectory,
                String applicationName,
                String serviceName,
                String hostName,
                MissingParametersAction missingParams,
                JObject defaultConfiguration = null,
                JObject defaultParameters = null
            )
        {

            DirectoryInfo baseDir = new DirectoryInfo(baseDirectory);
            var appDirectory = FileSystem.Instance.RedirectDirectory(
                Path.Combine(baseDir.FullName, applicationName)
            );
            var systemDirectory = Path.Combine(baseDir.FullName, ".system");
            if (!Directory.Exists(systemDirectory)) Directory.CreateDirectory(systemDirectory);

            var baseDirLen = Directory.GetParent(appDirectory).FullName.Length;
            String baseConfigFileName = Path.Combine(baseDir.FullName, "base.config");
            String defaultApplicationBaseConfigFileName = Path.Combine(systemDirectory, applicationName, serviceName, (hostName ?? "") + DateTime.Now.ToString("_yyyyMMdd_hhMMss_fff_") + ".config");
           
            String applicationBaseConfigFileName = Path.Combine(appDirectory, "base.config");
            String defaultDirectoryBaseConfigFileName = Path.Combine(appDirectory, "Default", "base.config");
            String serviceConfigFileName = Path.Combine(appDirectory, "Default", serviceName + ".config");

            //load standard config file
            List<ConfigFileInfo> configFiles = new List<ConfigFileInfo>();
            if (FileSystem.Instance.FileExists(baseConfigFileName))
                configFiles.Add(ConfigFileInfo.ForBase(FileSystem.Instance.GetFileContent(baseConfigFileName), baseConfigFileName.Substring(baseDirLen)));
            
            //check default configuration file
            if (defaultConfiguration != null) 
            {
                FileSystem.Instance.WriteFile(defaultApplicationBaseConfigFileName, defaultConfiguration.ToString());
                configFiles.Add(ConfigFileInfo.ForBase(FileSystem.Instance.GetFileContent(defaultApplicationBaseConfigFileName), defaultApplicationBaseConfigFileName.Substring(baseDirLen)));
                _logger.InfoFormat("Default File used. App {0} service {1} host {2} content {3}",
                    applicationName, serviceName, hostName, defaultConfiguration.ToString());
            }
            
            if (FileSystem.Instance.FileExists(applicationBaseConfigFileName))
                configFiles.Add(ConfigFileInfo.ForBase(FileSystem.Instance.GetFileContent(applicationBaseConfigFileName), applicationBaseConfigFileName.Substring(baseDirLen)));
            if (FileSystem.Instance.FileExists(defaultDirectoryBaseConfigFileName))
                configFiles.Add(ConfigFileInfo.ForBase(FileSystem.Instance.GetFileContent(defaultDirectoryBaseConfigFileName), defaultDirectoryBaseConfigFileName.Substring(baseDirLen)));
            if (FileSystem.Instance.FileExists(serviceConfigFileName))
                configFiles.Add(ConfigFileInfo.ForBase(FileSystem.Instance.GetFileContent(serviceConfigFileName), serviceConfigFileName.Substring(baseDirLen)));

            //load specific machine configuration files
            if (!String.IsNullOrEmpty(hostName))
            {
                String hostBaseConfigFileName = Path.Combine(appDirectory, hostName, "base.config");
                if (FileSystem.Instance.FileExists(hostBaseConfigFileName))
                    configFiles.Add(ConfigFileInfo.ForHostSpecific(FileSystem.Instance.GetFileContent(hostBaseConfigFileName), hostBaseConfigFileName.Substring(baseDirLen), hostName));
                String hostConfigFileName = Path.Combine(appDirectory, hostName, serviceName + ".config");
                if (FileSystem.Instance.FileExists(hostConfigFileName))
                    configFiles.Add(ConfigFileInfo.ForHostSpecific(FileSystem.Instance.GetFileContent(hostConfigFileName), hostBaseConfigFileName.Substring(baseDirLen), hostName));
            }
            if (configFiles.Count == 0)
            {
                throw new ConfigurationErrorsException("There are no valid config at directory: " + baseDirectory);
            }
            var baseConfigObject = JsonComposer.ComposeJsonContent(configFiles.ToArray());
            String defaultDirectoryBaseParametersFileName = Path.Combine(appDirectory, "Default", "parameters.config");
            //then load all parameter files.
            var parameterObject = GetParameterObject(
                baseDirectory,
                applicationName,
                serviceName,
                hostName,
                defaultDirectoryBaseParametersFileName,
                defaultConfiguration,
                defaultParameters);
            //need to return all parameters.

            baseConfigObject["jarvis-parameters"] = (JObject) JsonConvert.DeserializeObject(
                JsonConvert.SerializeObject( parameterObject));

            //Do the substitution
            ParameterManager.ReplaceResult replaceResult;

            ParameterManager parameterManager;
            if (missingParams == MissingParametersAction.Blank)
            {
                parameterManager= new ParameterManager(String.Empty);
            }
            else
            {
                parameterManager = new ParameterManager();
            }
            while ((replaceResult = parameterManager.ReplaceParameters(baseConfigObject, parameterObject)).HasReplaced)
            {
                //do nothing, everything is done by the replace parameters routine
            }
        
            if (replaceResult.MissingParams.Count > 0)
            {
                var missingParametersList = replaceResult.MissingParams.Aggregate((s1, s2) => s1 + ", " + s2);

                switch (missingParams)
                {
                    case MissingParametersAction.Throw:
                        throw new ConfigurationErrorsException("Missing parameters: " +
                            missingParametersList); 
                    case MissingParametersAction.Ignore:
                        baseConfigObject["jarvis-missing-parameters"] = new JArray( replaceResult.MissingParams);
                        _logger.DebugFormat("Missing Parameter List {0} but client ask to ignore", missingParametersList);
                        break;
                    case MissingParametersAction.Blank:
                        baseConfigObject["jarvis-missing-parameters"] = new JArray(replaceResult.MissingParams);
                        break;
                }
            }
            parameterManager.UnescapePercentage(baseConfigObject);

            //cleanup file 
            if (FileSystem.Instance.FileExists(defaultApplicationBaseConfigFileName))
            {
                FileSystem.Instance.DeleteFile(defaultApplicationBaseConfigFileName);
            }
            if (FileSystem.Instance.FileExists(defaultDirectoryBaseParametersFileName)) 
            {
                FileSystem.Instance.DeleteFile(defaultDirectoryBaseParametersFileName);
            }

            return new ComposedConfiguration(baseConfigObject, replaceResult);
        }

        public static JObject GetParameterObject
           (
               String baseDirectory,
               String applicationName,
               String serviceName,
               String hostName, 
               String defaultDirectoryBaseParametersFileName,
               JObject defaultConfiguration = null,
               JObject defaultParameters = null
           )
        {

            DirectoryInfo baseDir = new DirectoryInfo(baseDirectory);
            var appDirectory = FileSystem.Instance.RedirectDirectory(
                Path.Combine(baseDir.FullName, applicationName)
            );
            var systemDirectory = Path.Combine(baseDir.FullName, ".system");
            if (!Directory.Exists(systemDirectory)) Directory.CreateDirectory(systemDirectory);

            var baseDirLen = Directory.GetParent(appDirectory).FullName.Length;

            //Base file parameters.config
            String baseParametersFileName = Path.Combine(baseDir.FullName, "parameters.config");
            //base file for application, in base directory called parameters.applicationname.config
            String baseApplicationParametersFileName = Path.Combine(baseDir.FullName, "parameters." + applicationName + ".config");
            //parameters.config inside the application directory
            String applicationBaseParametersFileName = Path.Combine(appDirectory, "parameters.config");

            String defaultApplicationBaseParametersFileName = Path.Combine(systemDirectory, applicationName, serviceName, (hostName ?? "") + DateTime.Now.ToString("_yyyyMMdd_hhMMss_fff_") + ".parameters.config");

            String serviceParametersFileName = Path.Combine(appDirectory, "Default", serviceName + ".parameters.config");
            List<ConfigFileInfo> parametersFiles = new List<ConfigFileInfo>();
            //Lower precedence, base parameter file name.
            if (FileSystem.Instance.FileExists(baseParametersFileName))
                parametersFiles.Add(ConfigFileInfo.ForBase(FileSystem.Instance.GetFileContent(baseParametersFileName), baseParametersFileName.Substring(baseDirLen)));

            //check default parameters file passed by the client
            if (defaultParameters != null)
            {
                FileSystem.Instance.WriteFile(defaultApplicationBaseParametersFileName, defaultParameters.ToString());
                parametersFiles.Add(ConfigFileInfo.ForBase(FileSystem.Instance.GetFileContent(defaultApplicationBaseParametersFileName), defaultApplicationBaseParametersFileName.Substring(baseDirLen)));
                _logger.InfoFormat("Default Parameters used. App {0} service {1} host {2} content {3}",
                    applicationName, serviceName, hostName, defaultParameters.ToString());
            }
            //PAY ATTENTION: base application parameters file name should have higher precedence than client
            //passed parameter file.
            if (FileSystem.Instance.FileExists(baseApplicationParametersFileName))
                parametersFiles.Add(ConfigFileInfo.ForBase(FileSystem.Instance.GetFileContent(baseApplicationParametersFileName), baseApplicationParametersFileName.Substring(baseDirLen)));

            if (FileSystem.Instance.FileExists(applicationBaseParametersFileName))
                parametersFiles.Add(ConfigFileInfo.ForBase(FileSystem.Instance.GetFileContent(applicationBaseParametersFileName), applicationBaseParametersFileName.Substring(baseDirLen)));
            if (FileSystem.Instance.FileExists(defaultDirectoryBaseParametersFileName))
                parametersFiles.Add(ConfigFileInfo.ForBase(FileSystem.Instance.GetFileContent(defaultDirectoryBaseParametersFileName), defaultDirectoryBaseParametersFileName.Substring(baseDirLen)));
            if (FileSystem.Instance.FileExists(serviceParametersFileName))
                parametersFiles.Add(ConfigFileInfo.ForBase(FileSystem.Instance.GetFileContent(serviceParametersFileName), serviceParametersFileName.Substring(baseDirLen)));

            JObject parameterObject;
            if (parametersFiles.Count > 0)
            {
                parameterObject = JsonComposer.ComposeJsonContent(parametersFiles.ToArray());
            }
            else
            {
                parameterObject = new JObject();
            }

            //use base parameters 
            JObject sysParams = new JObject();
            sysParams.Add("appName", applicationName);
            sysParams.Add("serviceName", serviceName);
            sysParams.Add("hostName", hostName);
            parameterObject.Add("sys", sysParams);

            return parameterObject;
        }

        public static String GetResourceFile
            (
                String baseDirectory,
                String applicationName,
                String serviceName,
                String hostName,
                String resourceFileName
            )
        {

            String appDirectory = Path.Combine(baseDirectory, applicationName);
            var redirectedAppDirectory = FileSystem.Instance.RedirectDirectory(appDirectory);

            //Resource file are simply located in default director of the application or in the host override folder
            List<String> possibleLocationsOfResourceFile = new List<string>();
            possibleLocationsOfResourceFile.Add(Path.Combine(redirectedAppDirectory, hostName, serviceName, resourceFileName));
            possibleLocationsOfResourceFile.Add(Path.Combine(redirectedAppDirectory, hostName, resourceFileName));
            possibleLocationsOfResourceFile.Add(Path.Combine(redirectedAppDirectory, "Default", serviceName, resourceFileName));
            possibleLocationsOfResourceFile.Add(Path.Combine(redirectedAppDirectory, "Default", resourceFileName));
            String defaultDirectoryBaseParametersFileName = Path.Combine(appDirectory, "Default", "parameters.config");

            var parameterObject = GetParameterObject(
                  baseDirectory,
                  applicationName,
                  serviceName,
                  hostName,
                  defaultDirectoryBaseParametersFileName);

            String content = GetContentOfFirstExistingFile(possibleLocationsOfResourceFile);
            ParameterManager parameterManager = new ParameterManager();
            content = parameterManager.ReplaceParametersInString(content, parameterObject);

            return content;
        }

        private static String GetContentOfFirstExistingFile(IEnumerable<String> paths)
        {
            foreach (var path in paths)
            {
                if (FileSystem.Instance.FileExists(path))
                {
                    return FileSystem.Instance.GetFileContent(path);
                }
            }
            return null;
        }

        internal class ConfigFileInfo
        {
            public static ConfigFileInfo ForBase(String content, String fileName)
            {
                return new ConfigFileInfo() { FileContent = content, FileName = fileName };
            }

            public static ConfigFileInfo ForHostSpecific(String content, String fileName, String host)
            {
                return new ConfigFileInfo() { FileContent = content, FileName = fileName, Host = host };
            }

            public static implicit operator ConfigFileInfo(String content)
            {
                return ConfigFileInfo.ForBase(content, "null");
            }

            public String FileContent { get; set; }

            public String Host { get; set; }

            public String FileName { get; set; }
        }
    }
}
