using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Jarvis.ConfigurationService.Client.Support
{
    interface IEnvironment
    {
        /// <summary>
        /// Used to access a file on the network or to call a service that answer with
        /// a full json file.
        /// </summary>
        String DownloadFile(String address);

        String GetFileContent(String fileName);

        /// <summary>
        /// Retrieve the configuration for the client, looking for a file
        /// with extension .application
        /// </summary>
        /// <returns></returns>
        ClientConfiguration GetApplicationConfig();

        void SaveFile(string fileName, string content, bool ignoreErrors);

        /// <summary>
        /// Used to access current path of the application
        /// </summary>
        String GetCurrentPath();

        String GetMachineName();
        String GetEnvironmentVariable(String variableName);
    }

    internal class StandardEnvironment : IEnvironment
    {
        /// <summary>
        /// Used to access a file on the network or to call a service that answer with
        /// a full json file.
        /// </summary>
        public String DownloadFile(String address)
        {
            try
            {
                using (var wc = new WebClient())
                {
                    wc.Headers.Add(HttpRequestHeader.ContentType, "application/json");
                    return wc.DownloadString(address);
                }
            }
            catch (WebException ex)
            {
                if (ex.Response != null)
                {
                    var responseStream = ex.Response.GetResponseStream();

                    if (responseStream != null)
                    {
                        using (var reader = new StreamReader(responseStream))
                        {
                            var responseText = reader.ReadToEnd();
                            throw new ServerConfigurationException(
                                "web exception:" + ex.Message,
                                responseText,
                                ex);
                        }
                    }
                }
                throw new ConfigurationErrorsException("Error reading configuration File, server responded with exception: " + ex.Message);
            }

        }

        public String GetFileContent(String fileName)
        {
            if (!File.Exists(fileName))
            {
                return null;
            }
            return File.ReadAllText(fileName);
        }

        public ClientConfiguration GetApplicationConfig()
        {
            var actualFolder = GetCurrentPath();
            DirectoryInfo currentDirectory = new DirectoryInfo(actualFolder);
            do
            {
                var clientConfig = currentDirectory
                    .GetFiles("*.application")
                    .Select(f => ClientConfiguration.TryCreateConfigurationFromFile(f.FullName))
                    .Where(c => c != null)
                    .FirstOrDefault();

                if (clientConfig != null)
                {
                    return clientConfig;
                }
                currentDirectory = currentDirectory.Parent;
            } while (currentDirectory != null);
            throw new ConfigurationErrorsException(
                "Unable to find file with extension .application to find application name");
        }

        public void SaveFile(String fileName, String content, Boolean ignoreErrors)
        {
            try
            {
                File.WriteAllText(fileName, content);
            }
            catch (IOException)
            {
                if (!ignoreErrors) throw;
            }
        }

        /// <summary>
        /// Used to access current path of the application
        /// </summary>
        public String GetCurrentPath()
        {
            return AppDomain.CurrentDomain.SetupInformation.ApplicationBase;
        }

        public String GetMachineName()
        {
            return Environment.MachineName;
        }

        public String GetEnvironmentVariable(String variableName)
        {
            return Environment.GetEnvironmentVariable(variableName);
        }


    }
}
