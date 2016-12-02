using System.Globalization;
using System.Linq;
using System.Web.Http;

using EOls.EPiContentApi.Extensions;
using EOls.EPiContentApi.Interfaces;
using EOls.EPiContentApi.Util;

using EPiServer;
using EPiServer.Core;
using EPiServer.Globalization;
using EPiServer.Security;
using EPiServer.ServiceLocation;

using Newtonsoft.Json;

namespace EOls.EPiContentApi
{
    public class ContentController : ApiController
    {
        private IContentRepository Repo { get; } = ServiceLocator.Current.GetInstance<IContentRepository>();
        
        public IHttpActionResult Get(int id, string locale = null)
        {
            // Set locale by query or by EPiServer
            locale = locale ?? ContentLanguage.PreferredCulture.Name;
            
            // Set preferredCulture so blocks can user ContentLanguage.PreferredCulture.Name to get current locale
            ContentLanguage.PreferredCulture = CultureInfo.CreateSpecificCulture(locale);
            
            var page = Repo.Get<PageData>(new ContentReference(id), new LanguageSelector(locale));

            // If page does note exist return bad request
            if(page == null) return BadRequest();

            // Page is not published and user is not a authorized admin
            if (page.Status != VersionStatus.Published && !(new[] { "WebEditors", "WebAdmins", "Administrators" }).Any(s => PrincipalInfo.CurrentPrincipal.IsInRole(s)))
            {
                return Unauthorized(null);
            }

            return Json(ContentSerializer.Instance.Serialize(page), new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore });
        }
    }
}