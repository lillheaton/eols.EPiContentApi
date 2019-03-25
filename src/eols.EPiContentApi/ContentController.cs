using EPiSerializer;
using EPiServer;
using EPiServer.Core;
using EPiServer.Globalization;
using EPiServer.Logging;
using EPiServer.Security;
using EPiServer.ServiceLocation;
using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web.Http;

namespace EOls.EPiContentApi
{
    public class ContentController : ApiController
    {
        private static readonly ILogger Logger = LogManager.GetLogger();
        private readonly IContentRepository _contentRepository;
        private readonly LanguageResolver _languageResolver;
        private readonly IUpdateCurrentLanguage _updateCurrentLanguage;
        private readonly IContentSerializer _contentSerializer;

        public ContentController(
            IContentRepository contentRepository,
            LanguageResolver languageResolver,
            IUpdateCurrentLanguage updateCurrentLanguage,
            IContentSerializer contentSerializer)
        {
            _contentRepository = contentRepository;
            _languageResolver = languageResolver;
            _updateCurrentLanguage = updateCurrentLanguage;
            _contentSerializer = contentSerializer;
        }
        public ContentController() : this(
            ServiceLocator.Current.GetInstance<IContentRepository>(),
            ServiceLocator.Current.GetInstance<LanguageResolver>(),
            ServiceLocator.Current.GetInstance<IUpdateCurrentLanguage>(),
            ServiceLocator.Current.GetInstance<IContentSerializer>())
        {
        }
        
        private static T MeasureTime<T>(Func<T> action)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            T value = action();

            stopwatch.Stop();
            Logger.Debug($"EOLS.ContentAPI.SerializeTimeMS = {stopwatch.ElapsedMilliseconds}");

            return value;
        }

        private string MessureAndSerializeTarget(IContent target)
        {
            return MeasureTime(() =>
            {
                return _contentSerializer.Serialize(target);
            });
        }

        public HttpResponseMessage Get(int id, string locale = null)
        {
            locale = locale ?? _languageResolver.GetPreferredCulture().Name;
            _updateCurrentLanguage.UpdateLanguage(locale);
            
            if (_contentRepository
                .TryGet(
                    new ContentReference(id), 
                    LanguageSelector.Fallback(locale, true), 
                    out IContent target))
            {
                string json;

                switch(target)
                {
                    case PageData page when target is PageData:                        
                        if (page.Status != VersionStatus.Published && !(new[] { "WebEditors", "WebAdmins", "Administrators" }).Any(s => PrincipalInfo.CurrentPrincipal.IsInRole(s)))
                        {
                            return Request.CreateErrorResponse(HttpStatusCode.Unauthorized, "Unauthorized");
                        }
                        json = MessureAndSerializeTarget(page);
                        break;

                    default:
                        json = MessureAndSerializeTarget(target);
                        break;
                }                

                var response = Request.CreateResponse(HttpStatusCode.OK);
                response.Content = new StringContent(json, Encoding.UTF8, "application/json");
                return response;                
            }
            else
            {
                Logger.Warning($"Unable to get page with ID {id} on market {locale}");

                // If content does note exist return bad request
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "");
            }
        }
    }
}