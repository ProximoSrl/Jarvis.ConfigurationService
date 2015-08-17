using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jarvis.ConfigurationService.Host.Model
{
    public enum MissingParametersAction
    {
        Unknown = 0,
        Throw = 1,
        Ignore = 2,
        Blank = 3
    }
}
