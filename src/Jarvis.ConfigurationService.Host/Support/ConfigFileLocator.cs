using System;
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
                String serviceName
            )
        {

            DirectoryInfo baseDir = new DirectoryInfo(baseDirectory);
            var appDirectory = FileSystem.Instance.RedirectDirectory(
                Path.Combine(baseDir.FullName, applicationName)
            );


            String baseConfigFileName = Path.Combine(baseDir.FullName, "base.config");
            String applicationBaseConfigFileName = Path.Combine(appDirectory, "base.config");
            String serviceConfigFileName = Path.Combine(appDirectory, "Default", serviceName + ".config");

            List<String> configFiles = new List<string>();
            if (FileSystem.Instance.FileExists(baseConfigFileName)) 
                configFiles.Add(FileSystem.Instance.GetFileContent(baseConfigFileName));
            if (FileSystem.Instance.FileExists(applicationBaseConfigFileName)) 
                configFiles.Add(FileSystem.Instance.GetFileContent(applicationBaseConfigFileName));
            if (FileSystem.Instance.FileExists(serviceConfigFileName))
                configFiles.Add(FileSystem.Instance.GetFileContent(serviceConfigFileName));
            return ComposeJsonContent(configFiles.ToArray());
        }

        public static JObject ComposeJsonContent
            (
            params String[] jsonContent
            )
        {
            if (jsonContent.Length == 0) return null;
            JObject result = new JObject();
            foreach (string json in jsonContent)
            {
                JObject parsed = JObject.Parse(json);
                ComposeObject(parsed, result);
            }
            return result;
        }

        private static void ComposeObject(JObject parsed, JObject result)
        {
            foreach (var property in parsed)
            {
                if (property.Value is JObject &&
                    result[property.Key] is JObject)
                {
                    ComposeObject((JObject) property.Value, (JObject) result[property.Key]);
                }
                else
                {
                      result[property.Key] = property.Value;
                }
            }
        }
    }
}
