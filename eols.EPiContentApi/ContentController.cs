using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Results;

using EPiServer;
using EPiServer.Core;
using EPiServer.Globalization;
using EPiServer.Logging;
using EPiServer.Security;
using EPiServer.ServiceLocation;

namespace EOls.EPiContentApi
{
    public class ContentController : ApiController
    {
        private static readonly ILogger Logger = LogManager.GetLogger();

        private IContentRepository Repo { get; } = ServiceLocator.Current.GetInstance<IContentRepository>();        
        
        public IHttpActionResult Get(int id, string locale = null)
        {
            var type = typeof(Interfaces.IApiPropertyConverter<>);
            var generic = type.MakeGenericType(typeof(BlockData));

            var test = ServiceLocator.Current.GetAllInstances(generic);


            // Set locale by query or by EPiServer
            locale = locale ?? ContentLanguage.PreferredCulture.Name;
            
            // Set preferredCulture so blocks can user ContentLanguage.PreferredCulture.Name to get current locale
            ContentLanguage.PreferredCulture = CultureInfo.CreateSpecificCulture(locale);
            PageData page;

            try
            {
                page = Repo.Get<PageData>(new ContentReference(id), new LanguageSelector(locale));                
            }
            catch (Exception ex)
            {
                Logger.Error($"Unable to get page with ID {id} on market {locale} : {ex.Message}");

                // If page does note exist return bad request
                return BadRequest(); 
            }
            
            // Page is not published and user is not a authorized admin
            if (page.Status != VersionStatus.Published && !(new[] { "WebEditors", "WebAdmins", "Administrators" }).Any(s => PrincipalInfo.CurrentPrincipal.IsInRole(s)))
            {
                return Unauthorized(null);
            }

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            var serializedData = ContentSerializer.SerializePage(page);
            stopwatch.Stop();
            
            var response = Request.CreateResponse(HttpStatusCode.OK, serializedData);
            response.Headers.Add("SerializeTimeMS", stopwatch.ElapsedMilliseconds.ToString());

            Logger.Debug($"EOLS.ContentAPI.SerializeTimeMS = {stopwatch.ElapsedMilliseconds}");
            
            return new ResponseMessageResult(response);
        }
    }
}