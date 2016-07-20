using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jarvis.ConfigurationService.Host.Support;
using Topshelf;

namespace Jarvis.ConfigurationService.Host
{
    class Program
    {
        private static String serviceName = "JarvisConfigurationService";

        static int Main(string[] args)
        {
            var exitCode = HostFactory.Run(host =>
            {
                var overrideServiceName = ConfigurationManager.AppSettings["serviceName"];
                if (!String.IsNullOrEmpty(overrideServiceName))
                {
                    serviceName = overrideServiceName;
                }
                SetHeader();
                AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
                String baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                FileInfo finfo = new FileInfo(Path.Combine(baseDirectory, "log4net.config"));
                if (!finfo.Exists)
                {
                    throw new ApplicationException("Unable to find log4net.config file at location " + finfo.FullName);
                }
                host.UseLog4Net(finfo.FullName);

                host.Service<ConfigurationServiceBootstrapper>(service =>
                {
                    var uri = new Uri(ConfigurationManager.AppSettings["uri"]);

                    service.ConstructUsing(() => new ConfigurationServiceBootstrapper(uri));
                    service.WhenStarted(s => s.Start());
                    service.WhenStopped(s => s.Stop());
                });

                host.RunAsNetworkService();

                host.SetDescription("Jarvis Configuration Service");
                host.SetDisplayName("Jarvis - Configuration service");
                host.SetServiceName(serviceName);
            });

            return (int)exitCode;
        }

        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            String baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            FileInfo finfo = new FileInfo(Path.Combine(baseDirectory, "_lastError.txt"));
            File.WriteAllText(finfo.FullName, e.ExceptionObject.ToString());
            if (Environment.UserInteractive)
            {
                Console.WriteLine(e.ExceptionObject.ToString());
                System.Diagnostics.Process.Start(finfo.FullName);
            }
        }

        private static void SetHeader()
        {
            if (Environment.UserInteractive)
            {
                Console.Title = "Jarvis Configuration Manager Service";
                Console.BackgroundColor = ConsoleColor.DarkMagenta;
                if (!Console.IsOutputRedirected && !Console.IsErrorRedirected)
                {
                    Console.Clear();
                }

                Banner();
            }
        }

        private static void Banner()
        {
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine("===================================================================");
            Console.WriteLine("Jarvis Configuration Service - Proximo srl");
            Console.WriteLine("===================================================================");
            Console.WriteLine("  install                                -> Install service");
            Console.WriteLine("  uninstall                              -> Remove service");
            Console.WriteLine("  net start " + serviceName + "          -> Start Service");
            Console.WriteLine("  net stop  " + serviceName + "          -> Stop Service");
            Console.WriteLine("===================================================================");
            Console.WriteLine();
            Console.WriteLine();
        }
    }
}
