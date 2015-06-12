using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jarvis.ConfigurationService.Host.Model
{
    public class ServerStatusModel
    {
        public string BaseFolder { get; set; }
        public string[] Applications { get; set; }
        public string Version { get; set; }

        public String FileVersion { get; set; }

        public String InformationalVersion { get; set; }
    }
}
