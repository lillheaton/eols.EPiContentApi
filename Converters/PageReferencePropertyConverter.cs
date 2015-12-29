using EOls.EPiContentApi.Extensions;
using EOls.EPiContentApi.Interfaces;

using EPiServer;
using EPiServer.Core;
using EPiServer.ServiceLocation;

namespace EOls.EPiContentApi.Converters
{
    public class PageReferencePropertyConverter : IApiPropertyConverter<PageReference>
    {
        public object Convert(PageReference obj, object owner, string locale)
        {
            if (obj == null) return null;

            var repo = ServiceLocator.Current.GetInstance<IContentRepository>();
            var page = repo.Get<PageData>(obj, new LanguageSelector(locale));
            return ContentSerializer.Instance.Serialize(page);
        }
    }
}