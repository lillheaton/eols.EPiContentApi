using EOls.EPiContentApi.Interfaces;
using EPiServer.DataAbstraction;
using EPiServer.ServiceLocation;

namespace EOls.EPiContentApi.Converters
{
    public class PageTypePropertyConverter : IApiPropertyConverter<PageType>
    {
        public object Convert(PageType obj, object owner, string locale)
        {
            if (obj == null) return null;
            return
                new
                {
                    obj.ID,
                    obj.Name,
                    obj.FullName,
                    obj.ModelType,
                    obj.DefaultMvcController,
                    obj.DefaultMvcPartialView,
                    obj.DefaultWebFormTemplate
                };
        }
    }
}