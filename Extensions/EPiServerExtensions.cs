using System;
using EPiServer;
using EPiServer.Core;
using EPiServer.ServiceLocation;
using EPiServer.Web.Routing;

namespace EOls.EPiContentApi.Extensions
{
    public static class EPiServerExtensions
    {
        public static string GetFriendlyUrl(this ContentReference contentRef, bool abolute = false)
        {
            var urlResolver = ServiceLocator.Current.GetInstance<UrlResolver>();
            return urlResolver.GetVirtualPath(contentRef).GetUrl(abolute);
        }

        public static string GetContentApiUrl(this ContentReference contentRef, string locale)
        {
            return $"/api/{locale}/content/{contentRef.ID}";
        }

        public static string GetContentApiUrl(this PageReference pageRef, string locale)
        {
            return $"/api/{locale}/content/{pageRef.ID}";
        }
    }
}