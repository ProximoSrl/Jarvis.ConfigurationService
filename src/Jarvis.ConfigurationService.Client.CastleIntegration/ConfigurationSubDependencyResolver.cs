using Castle.MicroKernel;
using Castle.Windsor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jarvis.ConfigurationService.Client.CastleIntegration
{
    public class ConfigurationSubDependencyResolver : ISubDependencyResolver
    {
        static HashSet<IWindsorContainer> installedContainer = new HashSet<IWindsorContainer>();
        
        public static void InstallIntoContainer(IWindsorContainer container) 
        {
            if (!installedContainer.Contains(container))
            {
                installedContainer.Add(container);
                container.Kernel.Resolver.AddSubResolver(
                    new ConfigurationSubDependencyResolver());
            }
        }

        public bool CanResolve(
            Castle.MicroKernel.Context.CreationContext context, 
            ISubDependencyResolver contextHandlerResolver, 
            Castle.Core.ComponentModel model, 
            Castle.Core.DependencyModel dependency)
        {
            return dependency.TargetType == typeof(StringConfiguration); 
        }

        public object Resolve(
            Castle.MicroKernel.Context.CreationContext context, 
            ISubDependencyResolver contextHandlerResolver, 
            Castle.Core.ComponentModel model, 
            Castle.Core.DependencyModel dependency)
        {
            var configuration = ConfigurationServiceClient.Instance.GetSetting(dependency.DependencyKey);
            return new StringConfiguration()
            {
                Value = configuration,
            };
        }
    }
}
