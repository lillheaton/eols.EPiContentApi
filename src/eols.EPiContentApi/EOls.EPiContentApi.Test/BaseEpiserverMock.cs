using System.Security.Principal;

using EOls.EPiContentApi.Interfaces;
using EOls.EPiContentApi.Test.Mock;

using EPiServer;
using EPiServer.Core;
using EPiServer.Security;
using EPiServer.ServiceLocation;
using EPiServer.Web.Routing;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

namespace EOls.EPiContentApi.Test
{
    public abstract class BaseEpiserverMock
    {
        [TestInitialize]
        public void Init()
        {
            var contentRepository = new Mock<IContentRepository>();
            
            // Create mock page 10
            var page = new Mock<PageData>();
            page.Setup(s => s.LanguageBranch).Returns("en");
            page.Setup(s => s.ContentLink).Returns(new PageReference(10));
            contentRepository.Setup(s => s.Get<PageData>(It.IsAny<ContentReference>(), It.IsAny<LanguageSelector>())).Returns(page.Object);

            // Mock url resolver
            var urlResolver = new Mock<UrlResolver>();
            urlResolver.Setup(s => s.GetUrl(It.IsAny<ContentReference>())).Returns(string.Empty);

            var locator = new Mock<IServiceLocator>();
            locator.Setup(s => s.GetInstance<IContentRepository>()).Returns(contentRepository.Object);
            locator.Setup(s => s.GetInstance<UrlResolver>()).Returns(urlResolver.Object);

            // Mock CacheManager
            locator.Setup(s => s.GetInstance<ICacheManager>()).Returns(new MockCacheManager());

            ServiceLocator.SetLocator(locator.Object);

            // Mock Principal roles
            var role = new Mock<IPrincipal>();
            role.Setup(s => s.IsInRole("WebEditors")).Returns(true);
            PrincipalInfo.CurrentPrincipal = role.Object;
        }
    }
}