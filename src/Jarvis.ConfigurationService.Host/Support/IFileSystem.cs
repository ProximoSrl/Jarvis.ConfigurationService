using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jarvis.ConfigurationService.Host.Support
{
    /// <summary>
    /// Abstract the file system
    /// </summary>
    interface IFileSystem
    {
        string GetBaseDirectory();

        IEnumerable<String> GetDirectories(string baseDirectory);

        Boolean DirectoryExists(string folderName);
    }

    /// <summary>
    /// Default implementation of file system.
    /// </summary>
    public class StandardFileSystem : IFileSystem
    {
        private String BaseDirectory { get; set; }

        public StandardFileSystem(String baseDirectory)
        {
            BaseDirectory = baseDirectory;
        }

        public string GetBaseDirectory()
        {

            if (String.IsNullOrEmpty(BaseDirectory))
            {
                BaseDirectory = Path.Combine(
                    AppDomain.CurrentDomain.SetupInformation.ApplicationBase,
                    "ConfigurationStore"
                );
            }
            if (!Directory.Exists(BaseDirectory))
            {
                throw new ConfigurationErrorsException("Base directory " + BaseDirectory + " does not exists");
            }
            return BaseDirectory;
        }

        public IEnumerable<String> GetDirectories(String baseDirectory)
        {
            return Directory.GetDirectories(baseDirectory);
        }


        public Boolean DirectoryExists(string folderName)
        {
            return Directory.Exists(folderName);
        }
    }

    public static class FileSystem
    {
        internal static IFileSystem Instance { get; private set; }

        static FileSystem()
        {
            Instance = new StandardFileSystem(ConfigurationManager.AppSettings["baseConfigDirectory"]);
        }


        internal static DisposableAction Override(IFileSystem overrideFileSystem)
        {
            var original = Instance;
            Instance = overrideFileSystem;
            return new DisposableAction(() => Instance = original);
        }
    }
}
