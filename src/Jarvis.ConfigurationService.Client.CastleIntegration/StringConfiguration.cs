using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jarvis.ConfigurationService.Client.CastleIntegration
{
    public class StringConfiguration
    {
        public String Value { get; set; }

        public static implicit operator String(StringConfiguration configuration) 
        {
            return configuration.Value;
        }
    }
}
