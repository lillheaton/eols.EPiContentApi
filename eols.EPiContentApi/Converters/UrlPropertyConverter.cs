using EOls.EPiContentApi.Interfaces;
using EPiServer;
using EPiServer.ServiceLocation;

namespace EOls.EPiContentApi.Converters
{
    public class UrlPropertyConverter : IApiPropertyConverter<Url>
    {
        public object Convert(Url obj, object owner, string locale)
        {
            return obj?.ToString();
        }
    }
}