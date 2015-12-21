using EOls.EPiContentApi.Interfaces;
using EPiServer;

namespace EOls.EPiContentApi.Converters
{
    public class UrlConverter : IApiPropertyConverter<Url>
    {
        public object Convert(Url obj, string locale)
        {
            return obj?.ToString();
        }
    }
}