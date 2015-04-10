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
    /// Abstract the file system usage of Config Controller.
    /// </summary>
    interface IFileSystem
    {

        String GetBaseDirectory();

        String RedirectDirectory(String folderName);

        IEnumerable<String> GetDirectories(string baseDirectory);

        Boolean DirectoryExists(string folderName, Boolean followRedirects);

        IEnumerable<String> GetFiles(string appFolder, string filter);

        bool FileExists(string fileName);

        string GetFileContent(string fileName);

        void WriteFile(string fileName, string content);

        void DeleteFile(string defaultApplicationBaseConfigFileName);
    }

    /// <summary>
    /// Default implementation of file system.
    /// </summary>
    public class StandardFileSystem : IFileSystem
    {
        private String BaseDirectory { get; set; }

        public StandardFileSystem(String baseDirectory)
        {
            if (!String.IsNullOrEmpty(baseDirectory))
            {
                if (System.IO.Path.IsPathRooted(baseDirectory))
                {
                    BaseDirectory = baseDirectory;
                }
                else
                {
                    BaseDirectory = Path.Combine(
                        AppDomain.CurrentDomain.SetupInformation.ApplicationBase,
                        baseDirectory
                    );
                }
            }
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

        public IEnumerable<String> GetFiles(string appFolder, string filter) 
        {
            return Directory.GetFiles(appFolder, filter);
        }

        public bool FileExists(string fileName)
        {
            return File.Exists(fileName);
        }

        public string GetFileContent(string fileName)
        {
            return File.ReadAllText(fileName);
        }

        public Boolean DirectoryExists(string folderName, Boolean followRedirects)
        {
            var realDirExists = Directory.Exists(folderName);
            if (!realDirExists && followRedirects)
            {
                //directory does not exists, needs to find if there is a redirect file in one
                //of the previous folder
                String redirectedDir = RedirectDirectory(folderName);
                return !String.IsNullOrEmpty(redirectedDir) && Directory.Exists(redirectedDir);
            }

            return realDirExists;
        }

        public String RedirectDirectory(String folderName) 
        {
            String actualFolderName = folderName;
            Int32 lastSeparatorPathPosition = folderName.Length - 1;
            do
            {
                var redirectFileName = actualFolderName + ".redirect";
                if (File.Exists(redirectFileName)) 
                {
                    var redirectContent = File.ReadAllText(redirectFileName);
                    return Path.Combine(redirectContent, folderName.Substring(lastSeparatorPathPosition + 1));
                };
                lastSeparatorPathPosition = actualFolderName.LastIndexOf(Path.DirectorySeparatorChar);
                if (lastSeparatorPathPosition > 0)
                {
                    actualFolderName = actualFolderName.Substring(0, lastSeparatorPathPosition);
                }
            } while (lastSeparatorPathPosition > 0);
            return folderName;
        }

        public void WriteFile(string fileName, string content)
        {
            var info = new FileInfo(fileName);
            if (!info.Directory.Exists) info.Directory.Create();

            File.WriteAllText(fileName, content);
        }

        public void DeleteFile(String fileName) 
        {
            File.Delete(fileName);
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
