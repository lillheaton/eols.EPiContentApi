using System.Collections.Generic;
using System.Linq;

using EOls.EPiContentApi.Interfaces;

using EPiServer;
using EPiServer.Core;
using EPiServer.ServiceLocation;

namespace EOls.EPiContentApi.Converters
{
    public class ContentAreaPropertyConverter : IApiPropertyConverter<ContentArea>
    {
        public object Convert(ContentArea obj, object owner, string locale)
        {
            if (obj == null) return null;
            
            return GetContent(obj.Items.Select(s => s.ContentLink), locale).ToArray();
        }

        private IEnumerable<object> GetContent(IEnumerable<ContentReference> references, string locale)
        {
            var repo = ServiceLocator.Current.GetInstance<IContentRepository>();

            foreach (var contentRef in references)
            {
                var cache = ContentSerializer.GetCachedObject(contentRef, locale);
                if (cache != null)
                {
                    yield return cache;
                    continue;
                }

                if (contentRef is PageReference)
                {
                    yield return ContentSerializer.SerializePage(repo.Get<PageData>(contentRef, new LanguageSelector(locale)));
                }
                else
                {
                    yield return ContentSerializer.Serialize(repo.Get<IContent>(contentRef, new LanguageSelector(locale)), locale, true);
                }
            }
        } 
    }
}