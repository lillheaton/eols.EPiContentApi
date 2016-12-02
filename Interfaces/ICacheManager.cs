using EPiServer.Core;

namespace EOls.EPiContentApi.Interfaces
{
    public interface ICacheManager
    {
        void CacheObject<T>(T obj, ContentReference contentReference, string locale) where T : class;

        T GetObject<T>(ContentReference contentReference, string locale) where T : class;

        void RemoveCache(ContentReference contentReference, string locale);
    }
}
