using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Topshelf;

namespace Jarvis.ConfigurationService.Host
{
    class Program
    {
        static int Main(string[] args)
        {
            var exitCode = HostFactory.Run(host =>
            {
                host.UseOldLog4Net("log4net.config");

                host.Service<Bootstrapper>(service =>
                {
                    var uri = new Uri(ConfigurationManager.AppSettings["uri"]);


                    service.ConstructUsing(() => new Bootstrapper(uri));
                    service.WhenStarted(s => s.Start());
                    service.WhenStopped(s => s.Stop());
                });

                host.RunAsNetworkService();

                host.SetDescription("Jarvis Configuration Service");
                host.SetDisplayName("Jarvis - Configuration service");
                host.SetServiceName("JarvisConfigurationService");
            });

            return (int)exitCode;
        }
    }
}
