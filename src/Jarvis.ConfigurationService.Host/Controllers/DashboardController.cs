using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Routing;
using Jarvis.ConfigurationService.Host.Model;
using Jarvis.ConfigurationService.Host.Support;
using Microsoft.SqlServer.Server;
using log4net;

namespace Jarvis.ConfigurationService.Host.Controllers
{
    public class DashboardController : ApiController
    {
        private static ILog _logger = LogManager.GetLogger(typeof(DashboardController));

        private static String version;

        private static String informationalVersion;

        static DashboardController()
        {
           
        }
    }
}
