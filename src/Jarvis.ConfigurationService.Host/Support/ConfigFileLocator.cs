﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

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


            String baseConfigFileName = Path.Combine(baseDir.FullName, "base.config");
            String applicationBaseConfigFileName = Path.Combine(appDirectory, "base.config");
            String serviceConfigFileName = Path.Combine(appDirectory, "Default", serviceName + ".config");

            List<ConfigFileInfo> configFiles = new List<ConfigFileInfo>();
            if (FileSystem.Instance.FileExists(baseConfigFileName))
                configFiles.Add(ConfigFileInfo.ForBase(FileSystem.Instance.GetFileContent(baseConfigFileName)));
            if (FileSystem.Instance.FileExists(applicationBaseConfigFileName))
                configFiles.Add(ConfigFileInfo.ForBase(FileSystem.Instance.GetFileContent(applicationBaseConfigFileName)));
            if (FileSystem.Instance.FileExists(serviceConfigFileName))
                configFiles.Add(ConfigFileInfo.ForBase(FileSystem.Instance.GetFileContent(serviceConfigFileName)));

            if (!String.IsNullOrEmpty(hostName))
            {
                String hostConfigFileName = Path.Combine(appDirectory, hostName, serviceName + ".config");
                if (FileSystem.Instance.FileExists(hostConfigFileName))
                    configFiles.Add(ConfigFileInfo.ForHostSpecific(FileSystem.Instance.GetFileContent(hostConfigFileName), hostName));
            }
            return ComposeJsonContent(configFiles.ToArray());
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
                JObject parsed = JObject.Parse(fileInfo.FileContent);
                ComposeObject(parsed, fileInfo.Host, result);
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
            public static ConfigFileInfo ForBase(String content)
            {
                return new ConfigFileInfo() { FileContent = content };
            }

            public static ConfigFileInfo ForHostSpecific(String content, String host)
            {
                return new ConfigFileInfo() { FileContent = content, Host = host };
            }

            public static implicit operator ConfigFileInfo(String content) 
            {
                return ConfigFileInfo.ForBase(content);
            }

            public String FileContent { get; set; }

            public String Host { get; set; }
        }
    }
}