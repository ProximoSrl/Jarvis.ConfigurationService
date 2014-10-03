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

namespace Jarvis.ConfigurationService.Host.Controllers
{
    public class ConfigController : ApiController
    {
        private static ILog _logger = LogManager.GetLogger(typeof(ConfigController));

        [HttpGet]
        [Route("")]
        public ServerStatusModel Status()
        {
            string baseDirectory = FileSystem.Instance.GetBaseDirectory();
            string[] applications = FileSystem.Instance
                .GetDirectories(baseDirectory)
                .Select(Path.GetFileName)
                .ToArray();

            return new ServerStatusModel
            {
                BaseFolder = baseDirectory,
                Applications = applications,
                Version = Assembly.GetEntryAssembly().GetName().Version.ToString()
            };
        }

        [HttpGet]
        [Route("{appName}")]
        public HttpResponseMessage GetConfiguration(String appName)
        {
            var baseDirectory = FileSystem.Instance.GetBaseDirectory();
            var appFolder = Path.Combine(baseDirectory, appName);
            if (!FileSystem.Instance.DirectoryExists(appFolder))
                return Request.CreateErrorResponse(HttpStatusCode.NotFound, "App not found");

            string[] modules = FileSystem.Instance
                .GetDirectories(appFolder)
                .Select(Path.GetFileName)
                .ToArray();

            return Request.CreateResponse(HttpStatusCode.OK, modules);
        }

        [HttpGet]
        [Route("{appName}/{moduleName}/config.json")]
        public Object GetConfiguration(String appName, String moduleName)
        {
            var baseDirectory = FileSystem.Instance.GetBaseDirectory();
            return ConfigFileLocator.GetConfig(baseDirectory, appName, moduleName);
        }

       
    }
}
