using EOls.EPiContentApi.Interfaces;
using EPiServer.Core;
using EPiServer.ServiceLocation;

namespace EOls.EPiContentApi.Converters
{
    public class BlockDataPropertyConverter : IApiPropertyConverter<BlockData>
    {
        public object Convert(BlockData obj, object owner, string locale)
        {
            return obj != null ? ContentSerializer.Serialize(obj, locale) : null;
        }
    }
}
