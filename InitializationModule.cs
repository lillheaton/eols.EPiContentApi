using System.Web.Http;
using System.Web.Routing;

using EOls.EPiContentApi.Interfaces;

using EPiServer;
using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using EPiServer.Globalization;
using EPiServer.ServiceLocation;

namespace EOls.EPiContentApi
{
    [InitializableModule]
    public class InitializationModule : IInitializableModule
    {
        public void RegisterRoutes(RouteCollection routes)
        {
            routes.MapHttpRoute(
                name: "Content Locale Api",
                routeTemplate: "api/{locale}/content/{id}",
                defaults: new { controller = "Content", id = RouteParameter.Optional }
            );

            routes.MapHttpRoute(
                name: "Content Api",
                routeTemplate: "api/content/{id}",
                defaults: new { controller = "Content", id = RouteParameter.Optional }
            );
        }

        public void Initialize(InitializationEngine context)
        {
            RegisterRoutes(RouteTable.Routes);
            DataFactory.Instance.PublishedContent += Instance_PublishedContent;
        }
        
        public void Uninitialize(InitializationEngine context)
        {
            DataFactory.Instance.PublishedContent -= Instance_PublishedContent;
        }


        private void Instance_PublishedContent(object sender, ContentEventArgs e)
        {
            // Try to remove cached content for contentReference
            ServiceLocator.Current.GetInstance<ICacheManager>().RemoveCache(e.ContentLink, ContentLanguage.PreferredCulture.Name);
        }
    }
}