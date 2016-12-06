using EOls.EPiContentApi.Interfaces;
using EPiServer.Core;

namespace EOls.EPiContentApi.Converters
{
    public class BlockDataPropertyConverter : IApiPropertyConverter<BlockData>
    {
        public object Convert(ContentSerializer serializer, BlockData obj, object owner, string locale)
        {
            return obj != null ? serializer.ConvertToKeyValue(obj, locale) : null;
        }
    }
}
