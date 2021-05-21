using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Jarvis.ConfigurationService.Client.Support
{
    /// <summary>
    /// This is the configuration reader file, it reads content of 
    /// .application files, reading the internal configuration for
    /// the client.
    /// </summary>
    class ClientConfiguration
    {
        public static ClientConfiguration TryCreateConfigurationFromFile(String configFile) 
        {
            String content = File.ReadAllText(configFile);
            if (content.StartsWith("#jarvis-config"))
            {
                return new ClientConfiguration(content, configFile);
            }
            return null;
        }

        /// <summary>
        /// Expected content is
        /// #jarvis-configuration
        /// application-name:xxxx
        /// base-server-address : http://localhost:55555/
        /// </summary>
        /// <param name="configData"></param>
        internal ClientConfiguration(String configData, String configFileName)
        {
            ApplicationName = Path.GetFileNameWithoutExtension(configFileName);
            ApplicationFileLocation = Path.GetDirectoryName(configFileName);
            var lines = configData.Split('\n','\r');
            var validLines = lines
                .Where(s => !String.IsNullOrEmpty(s) && !s.StartsWith("#"))
                .Select(s => s.Trim('\n', '\r', ' '));
            foreach (var line in validLines)
            {
                var semicolonPosition = line.IndexOf(':');
                if (semicolonPosition  < 0) 
                {
                    //invalid configuration option
                    throw new ConfigurationException("Invalid configuration line " + line + " in config file " + configFileName);
                }
                var configName = line
                    .Substring(0, semicolonPosition)
                    .Trim();
                var normalized = configName
                    .ToLower()
                    .Replace("-", "");
                var property = this.GetType().GetProperty(
                    normalized,
                    BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                if (property == null) 
                {
                    throw new ConfigurationException("Invalid configuration value " + configName + " in config file " + configFileName);
                }
                property.SetValue(this, line.Substring(semicolonPosition + 1).Trim());
            }
        }

        public String ApplicationName { get; private set; }

        /// <summary>
        /// This configuration is used if the address of configuration
        /// service is not specified in the Environment Variable.
        /// </summary>
        public String BaseServerAddress { get; private set; }

        public String ApplicationFileLocation { get; private set; }
    }
}
