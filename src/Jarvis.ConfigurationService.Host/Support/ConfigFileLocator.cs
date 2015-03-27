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

namespace Jarvis.ConfigurationService.Host.Support
{
    /// <summary>
    /// Class used to phisically locate and compose json files.
    /// </summary>
    public static class ConfigFileLocator
    {
        public static Object GetConfig
            (
                String baseDirectory,
                String applicationName,
                String serviceName,
                String hostName
            )
        {

             DirectoryInfo baseDir = new DirectoryInfo(baseDirectory);
            var appDirectory = FileSystem.Instance.RedirectDirectory(
                Path.Combine(baseDir.FullName, applicationName)
            );

            var baseDirLen = Directory.GetParent(appDirectory).FullName.Length;
            String baseConfigFileName = Path.Combine(baseDir.FullName, "base.config");
            String applicationBaseConfigFileName = Path.Combine(appDirectory, "base.config");
            String defaultDirectoryBaseConfigFileName = Path.Combine(appDirectory, "Default", "base.config");
            String serviceConfigFileName = Path.Combine(appDirectory, "Default", serviceName + ".config");

            //load standard config file
            List<ConfigFileInfo> configFiles = new List<ConfigFileInfo>();
            if (FileSystem.Instance.FileExists(baseConfigFileName))
                configFiles.Add(ConfigFileInfo.ForBase(FileSystem.Instance.GetFileContent(baseConfigFileName), baseConfigFileName.Substring(baseDirLen)));
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

            //then load all parameter files.
            String baseParametersFileName = Path.Combine(baseDir.FullName, "parameters.config");
            String applicationBaseParametersFileName = Path.Combine(appDirectory, "parameters.config");
            String defaultDirectoryBaseParametersFileName = Path.Combine(appDirectory, "Default", "parameters.config");
            String serviceParametersFileName = Path.Combine(appDirectory, "Default", serviceName + ".parameters.config");
            List<ConfigFileInfo> parametersFiles = new List<ConfigFileInfo>();
            if (FileSystem.Instance.FileExists(baseParametersFileName))
                parametersFiles.Add(ConfigFileInfo.ForBase(FileSystem.Instance.GetFileContent(baseParametersFileName), baseParametersFileName.Substring(baseDirLen)));
            if (FileSystem.Instance.FileExists(applicationBaseParametersFileName))
                parametersFiles.Add(ConfigFileInfo.ForBase(FileSystem.Instance.GetFileContent(applicationBaseParametersFileName), applicationBaseParametersFileName.Substring(baseDirLen)));
            if (FileSystem.Instance.FileExists(defaultDirectoryBaseParametersFileName))
                parametersFiles.Add(ConfigFileInfo.ForBase(FileSystem.Instance.GetFileContent(defaultDirectoryBaseParametersFileName), defaultDirectoryBaseParametersFileName.Substring(baseDirLen)));
            if (FileSystem.Instance.FileExists(serviceParametersFileName))
                parametersFiles.Add(ConfigFileInfo.ForBase(FileSystem.Instance.GetFileContent(serviceParametersFileName), serviceParametersFileName.Substring(baseDirLen)));

            var baseConfigObject = JsonComposer.ComposeJsonContent(configFiles.ToArray());
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

            //Do the substitution
            ParameterManager.ReplaceResult replaceResult;
            while ((replaceResult = ParameterManager.ReplaceParameters(baseConfigObject, parameterObject)).HasReplaced)
            {
                //do nothing, everything is done by the replace parameters routine

            }
            if (replaceResult.MissingParams.Count > 0)
            {
                throw new ConfigurationErrorsException("Missing parameters: " +
                    replaceResult.MissingParams.Aggregate((s1, s2) => s1 + ", " + s2));
            }
            ParameterManager.UnescapePercentage(baseConfigObject);

            return baseConfigObject;
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

            DirectoryInfo baseDir = new DirectoryInfo(baseDirectory);
            String appDirectory = Path.Combine(baseDirectory, applicationName);
            var redirectedAppDirectory = FileSystem.Instance.RedirectDirectory(appDirectory);

            //Resource file are simply located in default director of the application or in the host override folder
            List<String> possibleLocationsOfResourceFile = new List<string>();
            possibleLocationsOfResourceFile.Add(Path.Combine(appDirectory, hostName, serviceName, resourceFileName));
            possibleLocationsOfResourceFile.Add(Path.Combine(appDirectory, hostName, resourceFileName));
            possibleLocationsOfResourceFile.Add(Path.Combine(appDirectory, "Default", serviceName, resourceFileName));
            possibleLocationsOfResourceFile.Add(Path.Combine(appDirectory, "Default", resourceFileName));

            if (!String.IsNullOrEmpty(redirectedAppDirectory))
            {
                possibleLocationsOfResourceFile.Add(Path.Combine(redirectedAppDirectory, "Default", serviceName, resourceFileName));
                possibleLocationsOfResourceFile.Add(Path.Combine(redirectedAppDirectory, "Default", resourceFileName));
            }

            return GetContentOfFirstExistingFile(possibleLocationsOfResourceFile);
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
