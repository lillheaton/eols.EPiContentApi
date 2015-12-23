using System;
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
        public object Convert(ContentArea obj, string locale)
        {
            if (obj == null) return null;
            var repo = ServiceLocator.Current.GetInstance<IContentRepository>();
            try
            {
                return
                    obj.Items.Select(s => repo.Get<ContentData>(s.ContentLink, new LanguageSelector(locale)))
                        .Select(
                            s =>
                            s is PageData
                                ? ContentSerializer.Instance.ConvertPage(s as PageData) as object
                                : ContentSerializer.Instance.ConvertToKeyValue(s, locale))
                        .ToArray();
            }
            catch (Exception e)
            {
                return
                    obj.ContentFragments.Select(s => repo.Get<ContentData>(s.ContentLink, new LanguageSelector(locale)))
                        .Select(
                            s =>
                            s is PageData
                                ? ContentSerializer.Instance.ConvertPage(s as PageData) as object
                                : ContentSerializer.Instance.ConvertToKeyValue(s, locale))
                        .ToArray();
            }
        }
    }
}