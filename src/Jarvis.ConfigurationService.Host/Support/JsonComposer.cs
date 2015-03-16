using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Jarvis.ConfigurationService.Host.Support
{
    static internal class JsonComposer
    {
        public static JObject ComposeJsonContent
            (
            params ConfigFileLocator.ConfigFileInfo[] jsonContent
            )
        {
            if (jsonContent.Length == 0) return null;
            JObject result = new JObject();
            foreach (ConfigFileLocator.ConfigFileInfo fileInfo in jsonContent)
            {
                try
                {
                    JObject parsed = JObject.Parse(fileInfo.FileContent);
                    ComposeObject(parsed, fileInfo.Host, result);
                }
                catch (JsonException ex)
                {
                    //unable to parse the file, we need to tell the user that the file is malformed.
                    throw new ApplicationException("Error reading config file " + fileInfo.FileName + ": " + ex.Message, ex);
                }
            }
            return result;
        }

        internal static void ComposeObject(JObject parsed, String host, JObject result)
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
    }
}