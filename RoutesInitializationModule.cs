using System.Web.Http;
using System.Web.Routing;
using EPiServer.Framework;
using EPiServer.Framework.Initialization;

namespace EOls.EPiContentApi
{
    [InitializableModule]
    public class RoutesInitializationModule : IInitializableModule
    {
        public void RegisterRoutes(RouteCollection routes)
        {
            routes.MapHttpRoute(
                name: "Content Api",
                routeTemplate: "api/content/{action}/{id}",
                defaults: new { controller = "Content", action = RouteParameter.Optional, id = RouteParameter.Optional }
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