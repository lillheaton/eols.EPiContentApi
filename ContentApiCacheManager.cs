using EPiServer;
using EPiServer.Core;

namespace EOls.EPiContentApi
{
    public class ContentApiCacheManager
    {
        private const string Key = "EOls.ContentApi.ContentID{0}.Locale{1}";

        public static void CacheObject<T>(T obj, ContentReference contentReference, string locale) where T : class 
        {
            CacheManager.Insert(string.Format(Key, contentReference.ID, locale).ToLowerInvariant(), obj);
        }

        public static T GetObject<T>(ContentReference contentReference, string locale) where T : class 
        {
            return CacheManager.Get(string.Format(Key, contentReference.ID, locale).ToLowerInvariant()) as T;
        }

        public static void RemoveCache(ContentReference contentReference, string locale)
        {
            CacheManager.Remove(string.Format(Key, contentReference.ID, locale).ToLowerInvariant());
        }
    }
}