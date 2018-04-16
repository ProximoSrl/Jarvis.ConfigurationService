using Newtonsoft.Json;
using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace Jarvis.ConfigurationService.Client.Support
{
    internal interface IEnvironment
    {
        /// <summary>
        /// Used to access a file on the network or to call a service that answer with
        /// a full json file.
        /// </summary>
        String DownloadFile(String address);

        /// <summary>
        /// Used to download a file from the server with POST passing a JSON payload
        /// as single argument
        /// </summary>
        /// <param name="address"></param>
        /// <param name="payload"></param>
        /// <returns></returns>
        String DownloadFile(String address, String payload);

        /// <summary>
        /// Execute a call to the server
        /// </summary>
        /// <param name="address"></param>
        /// <param name="payload"></param>
        /// <param name="method"></param>
        /// <returns></returns>
        String ExecuteRequest(String address, Object payload, String method);

        /// <summary>
        /// Get content of a resource file.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        String GetFileContent(String fileName);

        /// <summary>
        /// Retrieve the configuration for the client, looking for a file
        /// with extension .application
        /// </summary>
        /// <returns></returns>
        ClientConfiguration GetApplicationConfig();

        /// <summary>
        /// Save a file into storage folder, is used to store various data like lastgoodconfiguration.config
        /// file.
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="content"></param>
        /// <param name="ignoreErrors"></param>
        /// <param name="encrypt">If true encrypt data with current user DPAPI.</param>
        void SaveFile(string fileName, string content, bool ignoreErrors, Boolean encrypt);

        /// <summary>
        /// Save a file into storage folder, is used to store various data like lastgoodconfiguration.config
        /// file.
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="encrypted">If true data was saved using current user DPAPI.</param>
        String LoadFile(string fileName, Boolean encrypted);

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
            Func<WebClient, String> webClientFunc = wc =>
            {
                wc.Headers.Add(HttpRequestHeader.ContentType, "application/json");
                return wc.DownloadString(address);
            };
            return ExecuteDownload(webClientFunc);
        }

        public String DownloadFile(String address, String payload)
        {
            Func<WebClient, String> webClientFunc = wc =>
            {
                wc.Headers.Add(HttpRequestHeader.ContentType, "application/json");
                return wc.UploadString(address, payload);
            };
            return ExecuteDownload(webClientFunc);
        }

        public String ExecuteRequest(String address, Object payload, String method)
        {
            Func<WebClient, String> webClientFunc = wc =>
            {
                var json = JsonConvert.SerializeObject(payload);
                wc.Headers.Add(HttpRequestHeader.ContentType, "application/json");
                return wc.UploadString(address, method, json);
            };
            return ExecuteDownload(webClientFunc);
        }

        private static String ExecuteDownload(Func<WebClient, String> functor)
        {
            try
            {
                using (var wc = new WebClient())
                {
                    return functor(wc);
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
                    .FirstOrDefault(c => c != null);

                if (clientConfig != null)
                {
                    return clientConfig;
                }
                currentDirectory = currentDirectory.Parent;
            } while (currentDirectory != null);
            throw new ConfigurationErrorsException(
                "Unable to find file with extension .application to find application name");
        }

        public void SaveFile(String fileName, String content, Boolean ignoreErrors, Boolean encrypt)
        {
            try
            {
                if (encrypt)
                {
                    var bc = Encoding.UTF8.GetBytes(content);
                    var encrypted = ProtectedData.Protect(bc, GetEntropy(fileName), DataProtectionScope.CurrentUser);
                    File.WriteAllBytes(fileName, encrypted);
                }
                else
                {
                    File.WriteAllText(fileName, content);
                }
            }
            catch (IOException)
            {
                if (!ignoreErrors) throw;
            }
        }

        public String LoadFile(String fileName, Boolean encrypted)
        {
            if (encrypted)
            {
                var encryptedContent = File.ReadAllBytes(fileName);
                var decrypted = ProtectedData.Unprotect(encryptedContent, GetEntropy(fileName), DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(decrypted);
            }
            else
            {
                return File.ReadAllText(fileName);
            }
        }

        private byte[] GetEntropy(string fileName)
        {
            return Encoding.UTF7.GetBytes(fileName);
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
            if (String.IsNullOrEmpty(variableName)) return null;

            return Environment.GetEnvironmentVariable(variableName);
        }
    }
}
