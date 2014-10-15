using Jarvis.ConfigurationService.Host.Support;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace Jarvis.ConfigurationService.Host.Controllers
{
    public class HelpersController : ApiController
    {
        private static ILog _logger = LogManager.GetLogger(typeof(HelpersController));

        [HttpGet]
        [Route("support/encryption/generatekey")]
        public Jarvis.ConfigurationService.Host.Support.EncryptionUtils.EncryptionKey Status()
        {
            return EncryptionUtils.GenerateKey();
        }
    }
}
