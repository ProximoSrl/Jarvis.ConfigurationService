using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Routing;

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
        public Object GetConfiguration()
        {
            return new { Status = "Config" };
        }
    }
}
