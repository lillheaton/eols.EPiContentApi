using EOls.EPiContentApi.Interfaces;
using EPiServer.Core;

namespace EOls.EPiContentApi.Converters
{
    public class BlockDataPropertyConverter : IApiPropertyConverter<BlockData>
    {
        public object Convert(BlockData obj, string locale)
        {
            return obj != null ? ContentSerializer.Instance.ConvertToKeyValue(obj, locale) : null;
        }
    }
}
