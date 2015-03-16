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
            String appDirectory = Path.Combine(baseDirectory, applicationName);
            String redirectedAppDirectory = FileSystem.Instance.RedirectDirectory(appDirectory);

            var baseDirLen = Directory.GetParent(appDirectory).FullName.Length;
            var redirectedAppDirectoryDirLen =
                !String.IsNullOrEmpty(redirectedAppDirectory) ?
                Directory.GetParent(redirectedAppDirectory).FullName.Length : 0;
            String baseConfigFileName = Path.Combine(baseDir.FullName, "base.config");


            //load standard config file
            List<ConfigFileInfo> configFiles = new List<ConfigFileInfo>();
            if (FileSystem.Instance.FileExists(baseConfigFileName))
                configFiles.Add(ConfigFileInfo.ForBase(FileSystem.Instance.GetFileContent(baseConfigFileName), baseConfigFileName.Substring(baseDirLen)));

            LookForFilesInRedirectedFolder(serviceName, hostName, redirectedAppDirectory, configFiles);

            LookForFilesInStandardApplicationFolder(serviceName, hostName, appDirectory, baseDirLen, configFiles);

            if (configFiles.Count == 0)
            {
                throw new ConfigurationErrorsException("There are no valid config at directory: " + baseDirectory);
            }

            //then load all parameter files from 
            List<ConfigFileInfo> parametersFiles = new List<ConfigFileInfo>();
            LoadParameterFiles(
                serviceName,
                baseDir, 
                appDirectory, 
                redirectedAppDirectory,
                baseDirLen, 
                redirectedAppDirectoryDirLen, 
                hostName,
                parametersFiles);

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

        private static void LookForFilesInStandardApplicationFolder(String serviceName, String hostName, String appDirectory, int baseDirLen, List<ConfigFileInfo> configFiles)
        {
            String applicationBaseConfigFileName = Path.Combine(appDirectory, "base.config");
            String defaultDirectoryBaseConfigFileName = Path.Combine(appDirectory, "Default", "base.config");
            String serviceConfigFileName = Path.Combine(appDirectory, "Default", serviceName + ".config");
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
        }

        private static void LoadParameterFiles(
            String serviceName,
            DirectoryInfo baseDir,
            String appDirectory,
            String redirectDiretory,
            Int32 baseDirLen,
            Int32 baseRedirectedDirLength,
            String hostName,
            List<ConfigFileInfo> parametersFiles)
        {
            String baseParametersFileName = Path.Combine(baseDir.FullName, "parameters.config");
            if (FileSystem.Instance.FileExists(baseParametersFileName))
                parametersFiles.Add(ConfigFileInfo.ForBase(FileSystem.Instance.GetFileContent(baseParametersFileName), baseParametersFileName.Substring(baseDirLen)));

            if (!String.IsNullOrEmpty(redirectDiretory))
            {
                String applicationRedirectBaseParametersFileName = Path.Combine(redirectDiretory, "parameters.config");
                String defaultRedirectDirectoryBaseParametersFileName = Path.Combine(redirectDiretory, "Default", "parameters.config");
                String serviceRedirectParametersFileName = Path.Combine(redirectDiretory, "Default", serviceName + ".parameters.config");
                if (FileSystem.Instance.FileExists(applicationRedirectBaseParametersFileName))
                    parametersFiles.Add(ConfigFileInfo.ForBase(FileSystem.Instance.GetFileContent(applicationRedirectBaseParametersFileName), applicationRedirectBaseParametersFileName.Substring(baseRedirectedDirLength)));
                if (FileSystem.Instance.FileExists(defaultRedirectDirectoryBaseParametersFileName))
                    parametersFiles.Add(ConfigFileInfo.ForBase(FileSystem.Instance.GetFileContent(defaultRedirectDirectoryBaseParametersFileName), defaultRedirectDirectoryBaseParametersFileName.Substring(baseRedirectedDirLength)));
                if (FileSystem.Instance.FileExists(serviceRedirectParametersFileName))
                    parametersFiles.Add(ConfigFileInfo.ForBase(FileSystem.Instance.GetFileContent(serviceRedirectParametersFileName), serviceRedirectParametersFileName.Substring(baseRedirectedDirLength)));
            }

            String applicationBaseParametersFileName = Path.Combine(appDirectory, "parameters.config");
            String defaultDirectoryBaseParametersFileName = Path.Combine(appDirectory, "Default", "parameters.config");
            String serviceParametersFileName = Path.Combine(appDirectory, "Default", serviceName + ".parameters.config");
            if (FileSystem.Instance.FileExists(applicationBaseParametersFileName))
                parametersFiles.Add(ConfigFileInfo.ForBase(FileSystem.Instance.GetFileContent(applicationBaseParametersFileName), applicationBaseParametersFileName.Substring(baseDirLen)));
            if (FileSystem.Instance.FileExists(defaultDirectoryBaseParametersFileName))
                parametersFiles.Add(ConfigFileInfo.ForBase(FileSystem.Instance.GetFileContent(defaultDirectoryBaseParametersFileName), defaultDirectoryBaseParametersFileName.Substring(baseDirLen)));
            if (FileSystem.Instance.FileExists(serviceParametersFileName))
                parametersFiles.Add(ConfigFileInfo.ForBase(FileSystem.Instance.GetFileContent(serviceParametersFileName), serviceParametersFileName.Substring(baseDirLen)));

            if (!String.IsNullOrEmpty(hostName))
            {
                String hostBaseParametersFileName = Path.Combine(appDirectory, hostName, "parameters.config");
                if (FileSystem.Instance.FileExists(hostBaseParametersFileName))
                    parametersFiles.Add(ConfigFileInfo.ForBase(FileSystem.Instance.GetFileContent(hostBaseParametersFileName), hostBaseParametersFileName.Substring(baseDirLen)));
                String hostServiceParametersFileName = Path.Combine(appDirectory, hostName, serviceName + ".parameters.config");
                if (FileSystem.Instance.FileExists(hostServiceParametersFileName))
                    parametersFiles.Add(ConfigFileInfo.ForBase(FileSystem.Instance.GetFileContent(hostServiceParametersFileName), hostServiceParametersFileName.Substring(baseDirLen)));
            }
        }

        private static void LookForFilesInRedirectedFolder(String serviceName, String hostName, String redirectedAppDirectory, List<ConfigFileInfo> configFiles)
        {
            //if redirect is present, we need to honor redirected file name
            if (!String.IsNullOrEmpty(redirectedAppDirectory))
            {
                var baseRedirectDirLen = Directory.GetParent(redirectedAppDirectory).FullName.Length;
                String redirectedApplicationBaseConfigFileName = Path.Combine(redirectedAppDirectory, "base.config");
                String redirectedDefaultDirectoryBaseConfigFileName = Path.Combine(redirectedAppDirectory, "Default", "base.config");
                String redirectedServiceConfigFileName = Path.Combine(redirectedAppDirectory, "Default", serviceName + ".config");

                if (FileSystem.Instance.FileExists(redirectedApplicationBaseConfigFileName))
                    configFiles.Add(ConfigFileInfo.ForBase(FileSystem.Instance.GetFileContent(redirectedApplicationBaseConfigFileName), redirectedApplicationBaseConfigFileName.Substring(baseRedirectDirLen)));
                if (FileSystem.Instance.FileExists(redirectedDefaultDirectoryBaseConfigFileName))
                    configFiles.Add(ConfigFileInfo.ForBase(FileSystem.Instance.GetFileContent(redirectedDefaultDirectoryBaseConfigFileName), redirectedDefaultDirectoryBaseConfigFileName.Substring(baseRedirectDirLen)));
                if (FileSystem.Instance.FileExists(redirectedServiceConfigFileName))
                    configFiles.Add(ConfigFileInfo.ForBase(FileSystem.Instance.GetFileContent(redirectedServiceConfigFileName), redirectedServiceConfigFileName.Substring(baseRedirectDirLen)));

                if (!String.IsNullOrEmpty(hostName))
                {
                    String hostRedirectBaseConfigFileName = Path.Combine(redirectedAppDirectory, hostName, "base.config");
                    if (FileSystem.Instance.FileExists(hostRedirectBaseConfigFileName))
                        configFiles.Add(ConfigFileInfo.ForHostSpecific(FileSystem.Instance.GetFileContent(hostRedirectBaseConfigFileName),
                            hostRedirectBaseConfigFileName.Substring(baseRedirectDirLen), hostName));
                    String hostRedirectConfigFileName = Path.Combine(redirectedAppDirectory, hostName, serviceName + ".config");
                    if (FileSystem.Instance.FileExists(hostRedirectConfigFileName))
                        configFiles.Add(ConfigFileInfo.ForHostSpecific(FileSystem.Instance.GetFileContent(hostRedirectConfigFileName),
                            hostRedirectConfigFileName.Substring(baseRedirectDirLen), hostName));
                }
            }
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
