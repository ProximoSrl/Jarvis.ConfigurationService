﻿using System;
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
        String GetApplicationName();
        void SaveFile(String fileName, String content);

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
            catch (Exception ex)
            {
                return String.Empty;
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

        public String GetApplicationName()
        {
            var actualFolder = GetCurrentPath();
            DirectoryInfo currentDirectory = new DirectoryInfo(actualFolder);
            do
            {
                var applicationFile = currentDirectory
                    .GetFiles("*.application")
                    .FirstOrDefault();
                if (applicationFile != null)
                {
                    return Path.GetFileNameWithoutExtension(applicationFile.FullName);
                }
                currentDirectory = currentDirectory.Parent;
            } while (currentDirectory != null);
            throw new ConfigurationErrorsException(
                "Unable to find file with extension .application to find application name");
        }

        public void SaveFile(String fileName, String content)
        {
            File.WriteAllText(fileName, content);
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