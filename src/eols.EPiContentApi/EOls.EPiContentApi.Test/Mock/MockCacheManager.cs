using System.Collections.Generic;
using EOls.EPiContentApi.Interfaces;
using EPiServer.Core;

namespace EOls.EPiContentApi.Test.Mock
{
    public class MockCacheManager : ICacheManager
    {
        private Dictionary<string, object> Cache { get; }
        private const string Key = "EOls.ContentApiTest.ContentID{0}.Locale{1}";

        public MockCacheManager()
        {
            Cache = new Dictionary<string, object>();
        }

        public void CacheObject<T>(T obj, ContentReference contentReference, string locale) where T : class
        {
            this.Cache.Add(string.Format(Key, contentReference.ID, locale).ToLowerInvariant(), obj);
        }

        public T GetObject<T>(ContentReference contentReference, string locale) where T : class
        {
            object obj;
            this.Cache.TryGetValue(string.Format(Key, contentReference.ID, locale).ToLowerInvariant(), out obj);

            return obj as T;
        }

        public void RemoveCache(ContentReference contentReference, string locale)
        {
            this.Cache.Remove(string.Format(Key, contentReference.ID, locale).ToLowerInvariant());
        }
    }
}
