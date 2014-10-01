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
            List<DirectoryInfo> hierarchy = new List<DirectoryInfo>();
            hierarchy.Add(new DirectoryInfo(baseDirectory));
            hierarchy.Add(new DirectoryInfo(Path.Combine(hierarchy.Last().FullName, applicationName)));
            hierarchy.Add(new DirectoryInfo(Path.Combine(hierarchy.Last().FullName, serviceName)));
            List<String> configFiles = new List<string>();
            foreach (var directoryInfo in hierarchy)
            {
                if (!directoryInfo.Exists) break;
                var configFile = directoryInfo.GetFiles("config.json").SingleOrDefault();
                if (configFile != null) configFiles.Add(File.ReadAllText(configFile.FullName));
            }
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
