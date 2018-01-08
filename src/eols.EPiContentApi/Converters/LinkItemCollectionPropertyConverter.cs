using System.Linq;
using EOls.EPiContentApi.Interfaces;
using EPiServer.SpecializedProperties;
using EPiServer.ServiceLocation;

namespace EOls.EPiContentApi.Converters
{
    public class LinkItemCollectionPropertyConverter : IApiPropertyConverter<LinkItemCollection>
    {
        public object Convert(LinkItemCollection obj, object owner, string locale)
        {
            return obj?.Select(s => new { s.Title, s.Text, s.Href, s.Attributes, s.Target }).ToArray();
        }
    }
}