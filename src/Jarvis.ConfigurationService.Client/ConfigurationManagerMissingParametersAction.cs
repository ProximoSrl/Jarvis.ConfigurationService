using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jarvis.ConfigurationService.Client
{
    public enum ConfigurationManagerMissingParametersAction
    {
        Unknown = 0,
        Throw = 1,
        Ignore = 2,
        Blank = 3
    }
}
