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
    /// <summary>
    /// Controller used to handle configurations.
    /// </summary>
    [ExceptionHandlingAttribute]
    public class ConfigController : ApiController
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(ConfigController));

        private static readonly String version;

        private static readonly String fileVersion;

        private static readonly String informationalVersion;

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

        /// <summary>
        /// Get the status of the server, it returns all information about how many configuration 
        /// are stored in the system.
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// Get configurations for a singole application, this allows to render the 
        /// menu with a list of configurations/services for a single application.
        /// </summary>
        /// <param name="appName"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Get a configuration for a specific application/moduleName, optionally
        /// we can specify an hostname to have configuration specific for a specific
        /// hostname.
        /// </summary>
        /// <param name="appName"></param>
        /// <param name="moduleName"></param>
        /// <param name="hostName"></param>
        /// <param name="missingParametersAction"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("{appName}/{moduleName}/config.json/{hostName=}")]
        [Route("{appName}/{moduleName}.config/{hostName=}")]
        public Object GetConfiguration(String appName, String moduleName, String hostName = "", MissingParametersAction missingParametersAction = MissingParametersAction.Throw)
        {
            var baseDirectory = FileSystem.Instance.GetBaseDirectory();
            var configuration = ConfigFileLocator.GetConfig(baseDirectory, appName, moduleName, hostName, missingParametersAction);
            return configuration.Configuration;
        }

        /// <summary>
        /// This version is in post and accepts as the body the default value for 
        /// all the parameters of the configuration. This is useful if we have default
        /// values with the user application, and we do not want to upload the 
        /// default to the configuration service.
        /// </summary>
        /// <param name="def"></param>
        /// <param name="appName"></param>
        /// <param name="moduleName"></param>
        /// <param name="hostName"></param>
        /// <param name="missingParametersAction"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("{appName}/{moduleName}.config/{hostName=}")]
        public Object GetConfigurationWithDefault(
            [FromBody] GetConfigurationWithDefault def,
            String appName,
            String moduleName,
            String hostName = "",
            MissingParametersAction missingParametersAction = MissingParametersAction.Throw)
        {
            var baseDirectory = FileSystem.Instance.GetBaseDirectory();
            JObject defaultParameters = null;
            JObject defaultConfiguration = null;

            if (def != null)
            {
                defaultParameters = def.DefaultParameters;
                defaultConfiguration = def.DefaultConfiguration;
            }

            var config = ConfigFileLocator.GetConfig(
                baseDirectory,
                appName,
                moduleName,
                hostName,
                missingParametersAction,
                defaultConfiguration,
                defaultParameters);
            return config.Configuration;
        }

        /// <summary>
        /// Get a resource.
        /// </summary>
        /// <param name="appName"></param>
        /// <param name="moduleName"></param>
        /// <param name="fileName"></param>
        /// <param name="hostName"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("{appName}/resources/{moduleName}/{filename}/{hostName=}")]
        public HttpResponseMessage GetResource(String appName, String moduleName, String fileName, String hostName = "")
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
