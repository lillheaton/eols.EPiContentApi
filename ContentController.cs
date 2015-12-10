using System.Globalization;
using System.Web.Http;

using EOls.EPiContentApi.Extensions;

using EPiServer;
using EPiServer.Core;
using EPiServer.Globalization;
using EPiServer.ServiceLocation;

namespace EOls.EPiContentApi
{
    public class ContentController : ApiController
    {
        private IContentRepository Repo { get; } = ServiceLocator.Current.GetInstance<IContentRepository>();

        public IHttpActionResult Get(int id, string locale = null)
        {
            var rootRef = new ContentReference(id);

            // Return 
            if (!rootRef.Exist()) return BadRequest();

            // Set locale by query or by EPiServer
            locale = locale ?? ContentLanguage.PreferredCulture.Name;

            // Set preferredCulture so blocks can user ContentLanguage.PreferredCulture.Name to get current locale
            ContentLanguage.PreferredCulture = CultureInfo.CreateSpecificCulture(locale);
            
            var page = Repo.Get<PageData>(new ContentReference(id));

            return Ok(ContentSerializer.Serialize(page));
        }
    }
}