using EPiServer;
using EPiServer.Core;
using EPiServer.Globalization;
using EPiServer.Logging;
using EPiServer.Security;
using EPiServer.ServiceLocation;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Web.Http;

namespace EOls.EPiContentApi
{
    public class ContentController : ApiController
    {
        private static readonly ILogger Logger = LogManager.GetLogger();
        private readonly IContentRepository _contentRepository;
        private readonly LanguageResolver _languageResolver;

        public ContentController() : this(
            ServiceLocator.Current.GetInstance<IContentRepository>(),
            ServiceLocator.Current.GetInstance<LanguageResolver>())
        {
        }
        public ContentController(IContentRepository contentRepository, LanguageResolver languageResolver)
        {
            _contentRepository = contentRepository;
            _languageResolver = languageResolver;
        }

        private static void MeasureTime(Action action)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            action();

            stopwatch.Stop();
            Logger.Debug($"EOLS.ContentAPI.SerializeTimeMS = {stopwatch.ElapsedMilliseconds}");
        }
        
        public IHttpActionResult Get(int id, string locale = null)
        {
            // Set locale by query or by EPiServer
            locale = locale ?? _languageResolver.GetPreferredCulture().Name;
            
            IContent target;

            if(_contentRepository.TryGet<IContent>(new ContentReference(id), new LanguageSelector(locale), out target))
            {
                object returnObject = null;

                PageData page = target as PageData;
                if (page != null)
                {
                    // Page is not published and user is not a authorized admin
                    if (page.Status != VersionStatus.Published && !(new[] { "WebEditors", "WebAdmins", "Administrators" }).Any(s => PrincipalInfo.CurrentPrincipal.IsInRole(s)))
                    {
                        return Unauthorized(null);
                    }

                    MeasureTime(() =>
                    {
                        returnObject = ContentSerializer.SerializePage(page);
                    });
                }
                else
                {
                    MeasureTime(() =>
                    {
                        returnObject = ContentSerializer.Serialize(target, locale);
                    });
                }
                
                return Json(
                    returnObject, 
                    new JsonSerializerSettings
                    {
                        ContractResolver = new CamelCasePropertyNamesContractResolver(),
                        ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                    });
            }
            else
            {
                Logger.Warning($"Unable to get page with ID {id} on market {locale}");

                // If content does note exist return bad request
                return BadRequest();
            }
        }
    }
}