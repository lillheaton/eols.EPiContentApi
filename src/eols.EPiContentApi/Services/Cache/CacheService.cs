using EOls.EPiContentApi.Interfaces;
using EPiServer;
using EPiServer.Core;
using EPiServer.ServiceLocation;

namespace EOls.EPiContentApi.Services.Cache
{
    [ServiceConfiguration(ServiceType = typeof(ICacheService), Lifecycle = ServiceInstanceScope.Singleton)]
    public class CacheService : ICacheService
    {
        private const string Key = "EOls.ContentApi.ContentID{0}.Locale{1}";

        public CacheService() { }

        public void CacheObject<T>(T obj, ContentReference contentReference, string locale) where T : class 
        {
            return;
            //CacheManager.Insert(string.Format(Key, contentReference.ID, locale).ToLowerInvariant(), obj);
        }

        public T GetObject<T>(ContentReference contentReference, string locale) where T : class 
        {
            return null;
            //return CacheManager.Get(string.Format(Key, contentReference.ID, locale).ToLowerInvariant()) as T;
        }

        public void RemoveCache(ContentReference contentReference, string locale)
        {
            return;
            //CacheManager.Remove(string.Format(Key, contentReference.ID, locale).ToLowerInvariant());
        }
    }
}