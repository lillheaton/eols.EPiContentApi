using System.Linq;
using EOls.EPiContentApi.Interfaces;
using EPiServer.SpecializedProperties;

namespace EOls.EPiContentApi.Converters
{
    public class LinkItemCollectionPropertyConverter : IApiPropertyConverter<LinkItemCollection>
    {
        public object Convert(ContentSerializer serializer, LinkItemCollection obj, object owner, string locale)
        {
            return obj?.Select(s => new { s.Title, s.Text, s.Href, s.Attributes, s.Language, s.Target }).ToArray();
        }
    }
}