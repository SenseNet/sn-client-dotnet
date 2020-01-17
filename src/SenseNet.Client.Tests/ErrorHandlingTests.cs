using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SenseNet.Client.Tests
{
    [TestClass]
    public class ErrorHandlingTests
    {
        [ClassInitialize]
        public static void ClassInitializer(TestContext _)
        {
            Initializer.InitializeServer();
        }

        [TestMethod]
        public async Task ContentTypeIsNotAllowed()
        {
            var content = Content.CreateNew("/Root", "Memo", "memo01");
            var errorIsCorrect = false;

            try
            {
                await content.SaveAsync().ConfigureAwait(false);
            }
            catch (ClientException ex)
            {
                errorIsCorrect = ex.StatusCode == HttpStatusCode.InternalServerError &&
                                 ex.ErrorData.ExceptionType == typeof (InvalidOperationException).Name &&
                                 ex.Message.Contains("Cannot save the content") &&
                                 ex.Message.Contains("does not allow the type");
            }

            Assert.IsTrue(errorIsCorrect);
        }

        [TestMethod]
        public async Task ParentDoesNotExist()
        {
            var content = Content.CreateNew("/Root/abcd", "Memo", "memo01");
            var errorIsCorrect = false;

            try
            {
                await content.SaveAsync().ConfigureAwait(false);
            }
            catch (ClientException ex)
            {
                errorIsCorrect = ex.StatusCode == HttpStatusCode.NotFound;
            }

            Assert.IsTrue(errorIsCorrect);
        }

        [TestMethod]
        public async Task ParseException()
        {
            var webex = new WebException("error");
            var ce = await RESTCaller.GetClientExceptionAsync(webex, "url", HttpMethod.Post, "body").ConfigureAwait(false);

            Assert.AreEqual("url", ce.Data["Url"]);
            Assert.AreEqual("POST", ce.Data["Method"]);
            Assert.AreEqual("body", ce.Data["Body"]);
        }
    }
}
