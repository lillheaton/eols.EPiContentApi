using EPiServer;
using EPiServer.Core;

namespace EOls.EPiContentApi
{
    public class ContentApiCacheManager
    {
        private const string Key = "EOls.ContentApi.Page{0}.Locale{1}";

        public static void CacheObject<T>(T obj, PageData requestPage)
        {
            CacheManager.Insert(string.Format(Key, requestPage.ContentLink.ID, requestPage.LanguageBranch), obj);
        }


    }
}
