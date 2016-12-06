using System;
using System.Collections.Generic;
using System.Linq;

using EOls.EPiContentApi.Interfaces;
using EOls.EPiContentApi.Models;

using EPiServer;
using EPiServer.Core;
using EPiServer.ServiceLocation;

namespace EOls.EPiContentApi.Converters
{
    public class ContentAreaPropertyConverter : IApiPropertyConverter<ContentArea>
    {
        public object Convert(ContentSerializer serializer, ContentArea obj, object owner, string locale)
        {
            if (obj == null) return null;

            try
            {
                return GetContent(serializer, obj.Items.Select(s => s.ContentLink), locale).ToArray();
            }
            catch (Exception e)
            {
                return GetContent(serializer, obj.ContentFragments.Select(s => s.ContentLink), locale);
            }
        }

        private IEnumerable<object> GetContent(ContentSerializer serializer, IEnumerable<ContentReference> references, string locale)
        {
            var repo = ServiceLocator.Current.GetInstance<IContentRepository>();

            foreach (var contentRef in references)
            {
                var cache = serializer.GetCachedObject(contentRef, locale);
                if (cache != null)
                {
                    yield return cache;
                    continue;
                }

                if (contentRef is PageReference)
                {
                    yield return serializer.Serialize(repo.Get<PageData>(contentRef, new LanguageSelector(locale)));
                }
                else
                {
                    yield return serializer.Serialize(repo.Get<IContent>(contentRef, new LanguageSelector(locale)), locale, true);
                }
            }
        } 
    }
}