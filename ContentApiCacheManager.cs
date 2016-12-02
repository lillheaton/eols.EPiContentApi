using EOls.EPiContentApi.Interfaces;
using EPiServer;
using EPiServer.Core;

namespace EOls.EPiContentApi
{
    public class ContentApiCacheManager : ICacheManager
    {
        private const string Key = "EOls.ContentApi.ContentID{0}.Locale{1}";

        public ContentApiCacheManager() { }

        public void CacheObject<T>(T obj, ContentReference contentReference, string locale) where T : class 
        {
            CacheManager.Insert(string.Format(Key, contentReference.ID, locale).ToLowerInvariant(), obj);
        }

        public T GetObject<T>(ContentReference contentReference, string locale) where T : class 
        {
            return CacheManager.Get(string.Format(Key, contentReference.ID, locale).ToLowerInvariant()) as T;
        }

        public void RemoveCache(ContentReference contentReference, string locale)
        {
            CacheManager.Remove(string.Format(Key, contentReference.ID, locale).ToLowerInvariant());
        }
    }
}