using Jarvis.ConfigurationService.Host.Model;
using Jarvis.ConfigurationService.Host.Support;
using log4net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace Jarvis.ConfigurationService.Host.Controllers
{
    [ExceptionHandlingAttribute]
    public class EditConfigurationController : ApiController
    {
        private static ILog _logger = LogManager.GetLogger(typeof(ConfigController));

        /// <summary>
        /// Set and completely overwrite parameters for a given application
        /// </summary>
        /// <param name="appName">Name of the application.</param>
        /// <returns></returns>
        [HttpPut]
        [Route("api/parameters/{appName}")]
        public async Task<Object> UpdateParameters(
           String appName )
        {
            var content = await Request.Content.ReadAsStringAsync();
            string applicationParameterFile = GetApplicationParametersFileName(appName);
            var jsonObj = (JObject) JsonConvert.DeserializeObject(content);
            var stringJson = jsonObj.ToString();
            File.WriteAllText(applicationParameterFile, stringJson);
            return new { success = true };
        }

        [HttpPut]
        [Route("api/defaultparameters/{appName}/{hostName}")]
        public async Task<Object> AddDefaultParameters(
          String appName,
          String hostName)
        {
            var content = await Request.Content.ReadAsStringAsync();
            string applicationParameterFile = GetApplicationParametersFileName(appName);
            JObject actualParam;
            if (!File.Exists(applicationParameterFile))
            {
                actualParam = new JObject();
            }
            else
            {
                actualParam = (JObject)JsonConvert.DeserializeObject(File.ReadAllText(applicationParameterFile));
            }
            var jsonObj = (JObject)JsonConvert.DeserializeObject(content);
            JsonComposer.ComposeObject(jsonObj, hostName, actualParam);
            var stringJson = actualParam.ToString();
            File.WriteAllText(applicationParameterFile, stringJson);
            return new { success = true };
        }

        [HttpGet]
        [Route("api/parameters/{appName}")]
        public Object GetParameters(
          String appName)
        {
            string applicationParameterFile = GetApplicationParametersFileName(appName);
            if (!File.Exists(applicationParameterFile))
                return new { };

            return JsonConvert.DeserializeObject(File.ReadAllText(applicationParameterFile));
        }

        [HttpPut]
        [Route("api/applications/{appName}")]
        public Object AddApplication(
            [FromBody] AddApplication model,
            String appName)
        {
            var redirectFile = GetApplicationRedirectFileName(appName);
            var directoryConfig = GetApplicationDirectory(appName);
            if (File.Exists(redirectFile) || Directory.Exists(directoryConfig))
            {
                return new { success = false, error = "Application was already created" };
            }
            File.WriteAllText(redirectFile, model.RedirectFolder);
            return new { success = true };
        }

        private static string GetApplicationParametersFileName(string appName)
        {
            var baseDirectory = FileSystem.Instance.GetBaseDirectory();
            DirectoryInfo baseDir = new DirectoryInfo(baseDirectory);
            var applicationParameterFile = Path.Combine(baseDir.FullName, "parameters." + appName + ".config");
            return applicationParameterFile;
        }

        private static string GetApplicationRedirectFileName(string appName)
        {
            var baseDirectory = FileSystem.Instance.GetBaseDirectory();
            DirectoryInfo baseDir = new DirectoryInfo(baseDirectory);
            var applicationParameterFile = Path.Combine(baseDir.FullName, appName + ".redirect");
            return applicationParameterFile;
        }

        private static string GetApplicationDirectory(string appName)
        {
            var baseDirectory = FileSystem.Instance.GetBaseDirectory();
            DirectoryInfo baseDir = new DirectoryInfo(baseDirectory);
            var applicationConfigurationDirectory = Path.Combine(baseDir.FullName, appName);
            return applicationConfigurationDirectory;
        }
    }
}
