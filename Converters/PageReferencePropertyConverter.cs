using EOls.EPiContentApi.Extensions;
using EOls.EPiContentApi.Interfaces;

using EPiServer.Core;

namespace EOls.EPiContentApi.Converters
{
    public class PageReferencePropertyConverter : IApiPropertyConverter<PageReference>
    {
        public object Convert(PageReference obj, string locale)
        {
            return obj?.GetContentApiUrl(locale);
        }
    }
}