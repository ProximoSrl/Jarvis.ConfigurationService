using System;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.Owin;
using Owin;

[assembly: OwinStartup(typeof(Jarvis.ConfigurationService.Host.Support.Startup))]

namespace Jarvis.ConfigurationService.Host.Support
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureWebApi(app);
        }

        protected virtual void ConfigureWebApi(IAppBuilder appBuilder)
        {
            var config = new HttpConfiguration();

            // Web API configuration and services

            // Web API routes
            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
                );

            appBuilder.UseWebApi(config);
        }
    }
}
