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


        [HttpPut]
        [Route("api/parameters/{appName}")]
        public async Task<Object> UpdateParameters(
           String appName )
        {
            var content = await Request.Content.ReadAsStringAsync   ();
            string applicationParameterFile = GetApplicationParametersFileName(appName);
            var jsonObj = (JObject) JsonConvert.DeserializeObject(content);
            var stringJson = jsonObj.ToString();
            File.WriteAllText(applicationParameterFile, stringJson);
            return new { success = true };
        }

        [HttpGet]
        [Route("api/parameters/{appName}")]
        public async Task<Object> GetParameters(
          String appName)
        {
            string applicationParameterFile = GetApplicationParametersFileName(appName);
            if (!File.Exists(applicationParameterFile))
                return new { };

            return JsonConvert.DeserializeObject(File.ReadAllText(applicationParameterFile));
        }

        private static string GetApplicationParametersFileName(string appName)
        {
            var baseDirectory = FileSystem.Instance.GetBaseDirectory();
            DirectoryInfo baseDir = new DirectoryInfo(baseDirectory);
            var applicationParameterFile = Path.Combine(baseDir.FullName, "parameters." + appName + ".config");
            return applicationParameterFile;
        }
    }
}
