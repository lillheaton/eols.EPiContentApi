using System;
using EPiServer;
using EPiServer.Core;
using EPiServer.ServiceLocation;
using EPiServer.Web.Routing;

namespace EOls.EPiContentApi.Extensions
{
    public static class EPiServerExtensions
    {
        public static bool Exist(this ContentReference contentRef)
        {
            try
            {
                ServiceLocator.Current.GetInstance<IContentRepository>().Get<IContent>(contentRef);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static string GetFriendlyUrl(this ContentReference contentRef, bool abolute = false)
        {
            var urlResolver = ServiceLocator.Current.GetInstance<UrlResolver>();
            return urlResolver.GetVirtualPath(contentRef).GetUrl(abolute);
        }

        public static string GetContentApiUrl(this ContentReference contentRef)
        {
            return $"/api/content/get/{contentRef.ID}";
        }

        public static string GetContentApiUrl(this PageReference pageRef)
        {
            return $"/api/content/get/{pageRef.ID}";
        }
    }
}