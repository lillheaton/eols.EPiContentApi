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
        public ICacheManager CacheManager { get; } = ServiceLocator.Current.GetInstance<ICacheManager>();

        public object Convert(ContentArea obj, object owner, string locale)
        {
            if (obj == null) return null;

            try
            {
                return GetContent(obj.Items.Select(s => s.ContentLink), locale).ToArray();
            }
            catch (Exception e)
            {
                return GetContent(obj.ContentFragments.Select(s => s.ContentLink), locale);
            }
        }

        private IEnumerable<object> GetContent(IEnumerable<ContentReference> references, string locale)
        {
            var repo = ServiceLocator.Current.GetInstance<IContentRepository>();

            foreach (var contentRef in references)
            {
                if (contentRef is PageReference)
                {
                    var pageData = this.CacheManager.GetObject<ContentModel>(contentRef, locale);
                    if (pageData != null)
                    {
                        yield return pageData;
                        continue;
                    }

                    pageData = ContentSerializer.Instance.Serialize(repo.Get<PageData>(contentRef, new LanguageSelector(locale)));
                    this.CacheManager.CacheObject(pageData, contentRef, locale);
                    yield return pageData;
                }
                else
                {
                    var contentDict = this.CacheManager.GetObject<Dictionary<string, object>>(contentRef, locale);
                    if (contentDict != null)
                    {
                        yield return contentDict;
                        continue;
                    }

                    contentDict = ContentSerializer.Instance.ConvertToKeyValue(repo.Get<ContentData>(contentRef, new LanguageSelector(locale)), locale);
                    this.CacheManager.CacheObject(contentDict, contentRef, locale);
                    yield return contentDict;
                }
            }
        } 
    }
}