using EOls.EPiContentApi.Extensions;
using EOls.EPiContentApi.Models;
using EPiServer.Core;

namespace EOls.EPiContentApi
{
    // http://jondjones.com/dependency-injection-in-episerver-servicelocator-and-injected-explained/ maybe allow users to inject service

    public class ContentSerializer
    {
        public static ContentModel Serialize(PageData root)
        {
            return new ContentModel
            {
                ContentId = root.ContentLink.ID,
                Url = root.ContentLink.GetFriendlyUrl(),
                Name = root.Name,
                ContentTypeId = root.ContentTypeID,
                PageTypeName = root.PageTypeName
            };
        }
    }
}