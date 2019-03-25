using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using System.Web.Http;
using System.Web.Routing;

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
        }

        public void Uninitialize(InitializationEngine context)
        {            
        }
    }
}