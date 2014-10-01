using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace Jarvis.ConfigurationService.Host.Controllers
{
    public class ConfigController : ApiController
    {
        [HttpGet]
        public string Status()
        {
            return "ok";
        }
    }
}
