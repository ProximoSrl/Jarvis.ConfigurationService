using System;
using System.Collections.Generic;
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
        public static String GetConfig
            (
            String baseDirectory,
            String applicationName,
            String serviceName
            )
        {
            throw new NotImplementedException();
        }

        public static String ComposeJsonContent
            (
            params String[] jsonContent
            )
        {
            JObject result = new JObject();
            foreach (string json in jsonContent)
            {
                JObject parsed = JObject.Parse(json);
                ComposeObject(parsed, result);
            }
            return result.ToString();
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
