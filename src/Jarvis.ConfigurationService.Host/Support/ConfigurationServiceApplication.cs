using System;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.Owin;
using Owin;
using System.Net.Http.Headers;
using Microsoft.Owin.FileSystems;
using Microsoft.Owin.StaticFiles;
using System.IO;
using System.Linq;

[assembly: OwinStartup(typeof(Jarvis.ConfigurationService.Host.Support.ConfigurationServiceApplication))]

namespace Jarvis.ConfigurationService.Host.Support
{
    public class ConfigurationServiceApplication
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureWebApi(app);
            ConfigureAdmin(app);
        }

        protected virtual void ConfigureWebApi(IAppBuilder appBuilder)
        {
            var config = new HttpConfiguration();

            // Web API routes
            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "test/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
                );

            //Force always returning json
            config.Formatters.JsonFormatter.SupportedMediaTypes.Add(
                    new MediaTypeWithQualityHeaderValue("text/html")
                );

            appBuilder.UseWebApi(config);
            config.EnsureInitialized();
        }

        void ConfigureAdmin(IAppBuilder application)
        {
            var appFolder = FindAppRoot();

            var fileSystem = new PhysicalFileSystem(appFolder);

            var options = new FileServerOptions
            {
                EnableDirectoryBrowsing = true,
                FileSystem = fileSystem,
                EnableDefaultFiles = true
            };

            application.UseFileServer(options);
        }

        static string FindAppRoot()
        {
            var root = AppDomain.CurrentDomain.BaseDirectory
                .ToLowerInvariant()
                .Split(Path.DirectorySeparatorChar)
                .ToList();

            while (true)
            {
                var last = root.Last();
                if (last == String.Empty || last == "debug" || last == "release" || last == "bin")
                {
                    root.RemoveAt(root.Count - 1);
                    continue;
                }

                break;
            }

            root.Add("app");

            var appFolder = String.Join("" + Path.DirectorySeparatorChar, root);
            return appFolder;
        }
    }
}
