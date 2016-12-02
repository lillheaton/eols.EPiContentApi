using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace EOls.EPiContentApi.Test
{
    [TestClass]
    public class ContentControllerTests : BaseEpiserverMock
    {
        [TestMethod]
        public async Task GetReturnsOk()
        {
            var controller = new ContentController();

            controller.Request = new HttpRequestMessage();
            controller.Request.SetConfiguration(new HttpConfiguration());

            var response = await controller.Get(10, "en").ExecuteAsync(CancellationToken.None);
            var json = JsonConvert.DeserializeObject<dynamic>(await response.Content.ReadAsStringAsync());

            Assert.IsTrue(json["ContentId"] == 10);
        }
    }
}
