using Jarvis.ConfigurationService.Host.Support;
using log4net;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
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
        public EncryptionUtils.EncryptionKey GenerateKey()
        {
            return EncryptionUtils.GenerateKey();
        }

        [HttpPost]
        [Route("support/encryption/encrypt")]
        public Object Encrypt(EncryptionRequest request)
        {
            if (request == null || String.IsNullOrEmpty(request.StringToEncrypt)) 
            {
                return new
                {
                    success = false,
                    error = "Expecting a request in the form: {StringToEncrypt : 'the string you want to encrypt'}",
                };
            }
            String errMessage;
            var key = EncryptionUtils.GetDefaultEncryptionKey(out errMessage);
            if (!String.IsNullOrEmpty(errMessage)) 
            {
                return new
                {
                    success = false,
                    error = errMessage,
                };
            }
            String encrypted = EncryptionUtils.Encrypt(key.Key, key.IV, request.StringToEncrypt);
            return new 
            {
                success = true,
                encrypted = encrypted,
            };
        }
    }

    public class EncryptionRequest 
    {
        public String StringToEncrypt { get; set; }
    }
}
