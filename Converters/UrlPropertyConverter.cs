using EOls.EPiContentApi.Interfaces;
using EPiServer;

namespace EOls.EPiContentApi.Converters
{
    public class UrlPropertyConverter : IApiPropertyConverter<Url>
    {
        public object Convert(ContentSerializer serializer, Url obj, object owner, string locale)
        {
            return obj?.ToString();
        }
    }
}