using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Filters;

namespace Jarvis.ConfigurationService.Host.Controllers
{
    /// <summary>
    /// Allow to log all exceptions that are unhandled in api method execution.
    /// </summary>
    public class ExceptionHandlingAttribute : ExceptionFilterAttribute
    {
        private static ILog _logger = LogManager.GetLogger(typeof(ExceptionHandlingAttribute));

        public override void OnException(HttpActionExecutedContext context)
        {
            //Log Critical errors
            _logger.Error(String.Format("Exception in Action {0} of controller {1}",
                context.ActionContext.ActionDescriptor.ActionName,
                context.ActionContext.ControllerContext.ControllerDescriptor.ControllerName), context.Exception);
        }
    }
}
