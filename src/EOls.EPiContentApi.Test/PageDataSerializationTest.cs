using System;
using EPiServer;
using EPiServer.Core;
using EPiServer.DataAbstraction;
using EPiServer.ServiceLocation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json;

namespace EOls.EPiContentApi.Test
{
    [TestClass]
    public class PageDataSerializationTest
    {
        public class MyCustomPage : PageData
        {
            public string Foo { get; set; }            
        }

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            //var serviceLocator = new Mock<IServiceLocator>();
            //var mockRepository = new Mock<IContentRepository>();
            //var ContentTypeRepositoryock = new Mock<IContentTypeRepository>();

            //serviceLocator.Setup(x => x.GetInstance<IContentRepository>()).Returns(mockRepository.Object);
            //ServiceLocator.SetLocator(serviceLocator.Object);
        }

        [TestMethod]
        public void TestMethod1()
        {
            var test = new MyCustomPage { Foo = "kalle" };


            string json = ContentSerializer.Serialize(new MyCustomPage { Foo = "kalle" }, "en");
            Console.Write(json);

            var obj = JsonConvert.DeserializeObject<dynamic>(json);
            Assert.AreEqual("Kalle", obj.Foo.ToString());
        }
    }
}
