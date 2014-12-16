using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Configuration;

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

            List<ConfigFileInfo> configFiles = new List<ConfigFileInfo>();
            if (FileSystem.Instance.FileExists(baseConfigFileName))
                configFiles.Add(ConfigFileInfo.ForBase(FileSystem.Instance.GetFileContent(baseConfigFileName), baseConfigFileName.Substring(baseDirLen)));
            if (FileSystem.Instance.FileExists(applicationBaseConfigFileName))
                configFiles.Add(ConfigFileInfo.ForBase(FileSystem.Instance.GetFileContent(applicationBaseConfigFileName), applicationBaseConfigFileName.Substring(baseDirLen)));
            if (FileSystem.Instance.FileExists(defaultDirectoryBaseConfigFileName))
                configFiles.Add(ConfigFileInfo.ForBase(FileSystem.Instance.GetFileContent(defaultDirectoryBaseConfigFileName), defaultDirectoryBaseConfigFileName.Substring(baseDirLen)));
            if (FileSystem.Instance.FileExists(serviceConfigFileName))
                configFiles.Add(ConfigFileInfo.ForBase(FileSystem.Instance.GetFileContent(serviceConfigFileName), serviceConfigFileName.Substring(baseDirLen)));

            if (!String.IsNullOrEmpty(hostName))
            {
                String hostBaseConfigFileName = Path.Combine(appDirectory, hostName, "base.config");
                if (FileSystem.Instance.FileExists(hostBaseConfigFileName))
                    configFiles.Add(ConfigFileInfo.ForHostSpecific(FileSystem.Instance.GetFileContent(hostBaseConfigFileName),hostBaseConfigFileName.Substring(baseDirLen) , hostName));
                String hostConfigFileName = Path.Combine(appDirectory, hostName, serviceName + ".config");
                if (FileSystem.Instance.FileExists(hostConfigFileName))
                    configFiles.Add(ConfigFileInfo.ForHostSpecific(FileSystem.Instance.GetFileContent(hostConfigFileName), hostBaseConfigFileName.Substring(baseDirLen), hostName));
            }
            if (configFiles.Count == 0) 
            {
                throw new ConfigurationErrorsException("There are no valid config at directory: " + baseDirectory);
            }
            return ComposeJsonContent(configFiles.ToArray());
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
            var appDirectory = FileSystem.Instance.RedirectDirectory(
                Path.Combine(baseDir.FullName, applicationName)
            );

            //Resource file are simply located in default director of the application or in the host override folder
            String hostSpecificApplicationFileName = Path.Combine(appDirectory, hostName, resourceFileName);
            String hostSpecificServiceFileName = Path.Combine(appDirectory, hostName, serviceName, resourceFileName);

            String resourceApplicationFileName = Path.Combine(appDirectory, "Default", resourceFileName);
            String resourceServiceFileName = Path.Combine(appDirectory, "Default", serviceName, resourceFileName);
            return GetContentOfFirstExistingFile(hostSpecificServiceFileName, hostSpecificApplicationFileName, resourceServiceFileName, resourceApplicationFileName);
        }

        private static String GetContentOfFirstExistingFile(params String[] paths) 
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

        internal static JObject ComposeJsonContent
            (
            params ConfigFileInfo[] jsonContent
            )
        {
            if (jsonContent.Length == 0) return null;
            JObject result = new JObject();
            foreach (ConfigFileInfo fileInfo in jsonContent)
            {
                try
                {
                    JObject parsed = JObject.Parse(fileInfo.FileContent);
                    ComposeObject(parsed, fileInfo.Host, result);
                }
                catch (Newtonsoft.Json.JsonException ex)
                {
                    //unable to parse the file, we need to tell the user that the file is malformed.
                    throw new ApplicationException("Error reading config file " + fileInfo.FileName + ": " + ex.Message, ex);
                }
            }
            return result;
        }

        private static void ComposeObject(JObject parsed, String host, JObject result)
        {
            foreach (var property in parsed)
            {
                if (property.Value is JObject &&
                    result[property.Key] is JObject)
                {
                    ComposeObject((JObject)property.Value, host, (JObject)result[property.Key]);
                }
                else
                {
                    if (property.Key.StartsWith("$") && property.Key.EndsWith("$"))
                    {
                        var propertyName = property.Key.Trim('$');
                        String errorMessage;
                        var key = EncryptionUtils.GetDefaultEncryptionKey(host, out errorMessage);
                        if (String.IsNullOrEmpty(errorMessage))
                        {
                            try
                            {
                                result[propertyName] = EncryptionUtils.Decrypt(key.Key, key.IV, (String)property.Value);
                            }
                            catch (Exception ex)
                            {
                                result[propertyName] = "Unable to decrypt";
                            }
                        } 
                        else
                        {
                            result[propertyName] = "Unable to decrypt. Error: " + errorMessage;
                        }
                    }
                    else
                    {
                        result[property.Key] = property.Value;
                    }

                }
            }
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
