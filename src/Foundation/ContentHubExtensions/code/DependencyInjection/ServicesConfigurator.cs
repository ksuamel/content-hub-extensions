using Foundation.ContentHubExtensions.Helpers;
using Foundation.ContentHubExtensions.Services;
using Microsoft.Extensions.DependencyInjection;
using Sitecore.DependencyInjection;

namespace Foundation.ContentHubExtensions.DependencyInjection
{
    public class ServicesConfigurator : IServicesConfigurator
    {
        public void Configure(IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<CustomCmpHelper>();
            serviceCollection.AddSingleton<IItemIdService, ItemIdService>();
        }
    }
}