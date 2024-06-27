﻿using System.Net;

namespace SenseNet.Client.IntegrationTests.Legacy
{
    [TestClass]
    public class ErrorHandlingTests
    {
        [ClassInitialize]
        public static void ClassInitializer(TestContext context)
        {
            Initializer.InitializeServer(context);
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
            var restCaller = new DefaultRestCaller(null, null);

            var webEx = new HttpRequestException("error");
            var ce = await restCaller.GetClientExceptionAsync(webEx, "url", HttpMethod.Post, "body").ConfigureAwait(false);

            Assert.AreEqual("url", ce.Data["Url"]);
            Assert.AreEqual("POST", ce.Data["Method"]);
            Assert.AreEqual("body", ce.Data["Body"]);
        }
    }
}
