using EOls.EPiContentApi.Extensions;
using EOls.EPiContentApi.Interfaces;

using EPiServer;
using EPiServer.Core;
using EPiServer.ServiceLocation;
using EPiServer.Web.Routing;

namespace EOls.EPiContentApi.Converters
{
    public class ContentReferencePropertyConverter : IApiPropertyConverter<ContentReference>
    {
        public object Convert(ContentReference obj, string locale)
        {
            if (obj == null) return null;

            var content = ServiceLocator.Current.GetInstance<IContentRepository>().Get<IContent>(obj, new LanguageSelector(locale));
            var media = content as MediaData;
            if (media != null)
            {
                return new { media.Name, media.Thumbnail, media.MimeType, Url = UrlResolver.Current.GetUrl(media) };
            }

            return obj.GetContentApiUrl(locale);
        }
    }
}