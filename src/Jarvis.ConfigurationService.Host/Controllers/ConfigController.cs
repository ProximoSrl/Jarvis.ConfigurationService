using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Routing;
using Jarvis.ConfigurationService.Host.Model;
using Jarvis.ConfigurationService.Host.Support;
using Microsoft.SqlServer.Server;
using log4net;
using Newtonsoft.Json.Linq;
using System.Diagnostics;

namespace Jarvis.ConfigurationService.Host.Controllers
{
    [ExceptionHandlingAttribute]
    public class ConfigController : ApiController
    {
        private static ILog _logger = LogManager.GetLogger(typeof(ConfigController));

        private static String version;

        private static String fileVersion;

        private static String informationalVersion;

        static ConfigController()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            version = assembly.GetName().Version.ToString();
            
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            fileVersion = fvi.FileVersion;

            var informationalAttribute = Attribute
                .GetCustomAttribute(
                    Assembly.GetExecutingAssembly(),
                    typeof(AssemblyInformationalVersionAttribute))
                as AssemblyInformationalVersionAttribute;
            if (informationalAttribute != null)
                informationalVersion = informationalAttribute.InformationalVersion;
        }

        [HttpGet]
        [Route("status")]
        public ServerStatusModel Status()
        {
            string baseDirectory = FileSystem.Instance.GetBaseDirectory();
            var applicationsDir = FileSystem.Instance
                .GetDirectories(baseDirectory)
                .Select(Path.GetFileName)
                .Where(d => !d.StartsWith("."));
            var redirectedApps = FileSystem.Instance
                .GetFiles(baseDirectory, "*.redirect")
                .Select(f => Path.GetFileNameWithoutExtension(f));

            return new ServerStatusModel
            {
                BaseFolder = baseDirectory,
                Applications = applicationsDir.Union(redirectedApps).ToArray(),
                Version = version,
                FileVersion = fileVersion,
                InformationalVersion = informationalVersion,
            };
        }

        [HttpGet]
        [Route("{appName}/status")]
        public HttpResponseMessage GetConfiguration(String appName)
        {
            var baseDirectory = FileSystem.Instance.GetBaseDirectory();
            var appFolder = Path.Combine(baseDirectory, appName, "Default");
            var redirected = FileSystem.Instance.RedirectDirectory(appFolder) ??
                appFolder;
            if (!FileSystem.Instance.DirectoryExists(redirected, false))
                return Request.CreateErrorResponse(HttpStatusCode.NotFound, "App not found");

            string[] modules = FileSystem.Instance
                .GetFiles(redirected, "*.config")
                .Select(Path.GetFileNameWithoutExtension)
                .Where(p => !"base".Equals(p, StringComparison.OrdinalIgnoreCase))
                .ToArray();

            return Request.CreateResponse(HttpStatusCode.OK, modules);
        }

        [HttpGet]
        [Route("{appName}/{moduleName}/config.json/{hostName=}")]
        [Route("{appName}/{moduleName}.config/{hostName=}")]
        public Object GetConfiguration(String appName, String moduleName, String hostName = "")
        {
            var baseDirectory = FileSystem.Instance.GetBaseDirectory();
            return ConfigFileLocator.GetConfig(baseDirectory, appName, moduleName, hostName);
        }

        [HttpPost]
        [Route("{appName}/{moduleName}.config/{hostName=}")]
        public Object GetConfigurationWithDefault(
            [FromBody] GetConfigurationWithDefault def,
            String appName, 
            String moduleName,
            String hostName = "")
        {
            var baseDirectory = FileSystem.Instance.GetBaseDirectory();
            JObject defaultParameters  =null;
            JObject defaultConfiguration = null;
            if (def != null) 
            {
                defaultParameters = def.DefaultParameters;
                defaultConfiguration = def.DefaultConfiguration;
            }
            return ConfigFileLocator.GetConfig(
                baseDirectory, 
                appName, 
                moduleName, 
                hostName,
                defaultConfiguration,
                defaultParameters);
        }

        [HttpGet]
        [Route("{appName}/resources/{moduleName}/{filename}/{hostName=}")]
        public HttpResponseMessage GetConfiguration(String appName, String moduleName, String fileName, String hostName = "")
        {
            var baseDirectory = FileSystem.Instance.GetBaseDirectory();
            var resourceContent = ConfigFileLocator.GetResourceFile(baseDirectory, appName, moduleName, hostName, fileName);
            return
                new HttpResponseMessage()
                {
                    Content = new StringContent(resourceContent, Encoding.UTF8, "text/html")
                };
        }
    }
}
