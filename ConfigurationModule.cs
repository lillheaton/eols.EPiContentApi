using EOls.EPiContentApi.Interfaces;
using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using EPiServer.ServiceLocation;

namespace EOls.EPiContentApi
{
    [InitializableModule]
    public class ConfigurationModule : IConfigurableModule
    {
        public void Initialize(InitializationEngine context)
        {
        }

        public void Uninitialize(InitializationEngine context)
        {
        }

        public void ConfigureContainer(ServiceConfigurationContext context)
        {
            context.Services.Add(typeof(ICacheManager), new ContentApiCacheManager());
            context.Services.Add(typeof(IPropertyConverterManager), new PropertyConverterManager());
        }
    }
}
