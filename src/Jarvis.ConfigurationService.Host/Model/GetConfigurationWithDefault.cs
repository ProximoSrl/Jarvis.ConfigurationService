using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jarvis.ConfigurationService.Host.Model
{
    public class GetConfigurationWithDefault
    {
        public JObject DefaultConfiguration { get; set; }

        public JObject DefaultParameters { get; set; }
    }
}
