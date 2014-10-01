using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Routing;
using Jarvis.ConfigurationService.Host.Support;
using Microsoft.SqlServer.Server;

namespace Jarvis.ConfigurationService.Host.Controllers
{
    public class ConfigController : ApiController
    {
        [HttpGet]
        [Route("status")]
        public Object Status()
        {
            return new {Status = "ok"};
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
