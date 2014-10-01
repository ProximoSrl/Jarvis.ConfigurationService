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

namespace Jarvis.ConfigurationService.Host.Controllers
{
    public class ConfigController : ApiController
    {
        [HttpGet]
        [Route("")]
        public ServerStatusModel Status()
        {
            string baseDirectory = GetBaseDirectory();
            string[] applications = Directory
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
            var baseDirectory = GetBaseDirectory();
            var appFolder = Path.Combine(baseDirectory, appName);
            if (!Directory.Exists(appFolder))
                return Request.CreateErrorResponse(HttpStatusCode.NotFound, "App not found");

            string[] modules = Directory
                .GetDirectories(appFolder)
                .Select(Path.GetFileName)
                .ToArray();

            return Request.CreateResponse(HttpStatusCode.OK, modules);
        }

        [HttpGet]
        [Route("{appName}/{moduleName}/config.json")]
        public Object GetConfiguration(String appName, String moduleName)
        {
            var baseDirectory = GetBaseDirectory();
            return ConfigFileLocator.GetConfig(baseDirectory, appName, moduleName);
        }

        static string GetBaseDirectory()
        {
            var baseDirectory = ConfigurationManager.AppSettings["baseConfigDirectory"];
            if (String.IsNullOrEmpty(baseDirectory))
            {
                baseDirectory = Path.Combine(
                    AppDomain.CurrentDomain.SetupInformation.ApplicationBase,
                    "ConfigurationStore"
                );
            }
            if (!Directory.Exists(baseDirectory))
            {
                throw new ConfigurationErrorsException("Base directory " + baseDirectory + " does not exists");
            }
            return baseDirectory;
        }
    }
}
