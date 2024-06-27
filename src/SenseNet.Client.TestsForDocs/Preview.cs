using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Client.TestsForDocs.Infrastructure;
using SenseNet.Diagnostics;

namespace SenseNet.Client.TestsForDocs
{
    [TestClass]
    public class Preview : ClientIntegrationTestBase
    {
        /* ====================================================================================== Main */

        /// <tab category="preview" article="previews" example="getPageCount" />
        [TestMethod]
        [Description("Get page count")]
        public async Task Docs_Preview_Main_GetPageCount()
        {
            //UNDONE:Docs2: not implemented
            Assert.Inconclusive();
            SnTrace.Test.Write(">>>> ACT");
            /*<doc>*/
            /*</doc>*/
            SnTrace.Test.Write(">>>> ACT END");



            Assert.Inconclusive();
            // ACTION for doc
            var result = await RESTCaller.GetResponseStringAsync(
                "/Root/Content/IT/Document_Library/Calgary/BusinessPlan.docx", "GetPageCount", HttpMethod.Post);
            //Console.WriteLine(result);

            // ASSERT
            Assert.Inconclusive();
        }

        /// <tab category="preview" article="previews" example="checkPreviews" />
        [TestMethod]
        [Description("Check previews")]
        public async Task Docs_Preview_Main_CheckPreviews()
        {
            //UNDONE:Docs2: not implemented
            Assert.Inconclusive();
            SnTrace.Test.Write(">>>> ACT");
            /*<doc>*/
            /*</doc>*/
            SnTrace.Test.Write(">>>> ACT END");



            Assert.Inconclusive();
            // ACTION for doc
            var body = @"models=[{
                ""generateMissing"": false}]";
            var result = await RESTCaller.GetResponseStringAsync(
                "/Root/Content/IT/Document_Library/Calgary/BusinessPlan.docx", "CheckPreviews",
                HttpMethod.Post, body);

            //Console.WriteLine(result);

            // ASSERT
            Assert.Inconclusive();
        }

        /// <tab category="preview" article="previews" example="regeneratePreviews" />
        [TestMethod]
        [Description("Regenerate previews")]
        public async Task Docs_Preview_Main_RegeneratePreviews()
        {
            //UNDONE:Docs2: not implemented
            Assert.Inconclusive();
            SnTrace.Test.Write(">>>> ACT");
            /*<doc>*/
            /*</doc>*/
            SnTrace.Test.Write(">>>> ACT END");



            Assert.Inconclusive();
            // ACTION for doc
            await RESTCaller.GetResponseStringAsync(
                "/Root/Content/IT/Document_Library/Calgary/BusinessPlan.docx", "RegeneratePreviews",
                HttpMethod.Post);

            // ASSERT
            Assert.Inconclusive();
        }

        /// <tab category="preview" article="previews" example="addComment" />
        [TestMethod]
        [Description("Add comment")]
        public async Task Docs_Preview_Main_AddComment()
        {
            //UNDONE:Docs2: not implemented
            Assert.Inconclusive();
            SnTrace.Test.Write(">>>> ACT");
            /*<doc>*/
            /*</doc>*/
            SnTrace.Test.Write(">>>> ACT END");



            Assert.Inconclusive();
            // ACTION for doc
            var body = @"models=[{
                ""page"": 3,
                ""x"": 100,
                ""y"": 100,
                ""text"": ""Lorem ipsum dolor sit amet""}]";
            var result = await RESTCaller.GetResponseStringAsync(
                "/Root/Content/IT/Document_Library/Calgary/BusinessPlan.docx", "AddPreviewComment",
                HttpMethod.Post, body);
            //Console.WriteLine(result);

            // ASSERT
            Assert.Inconclusive();
        }

        /// <tab category="preview" article="previews" example="getComments" />
        [TestMethod]
        [Description("Get comments for a page")]
        public async Task Docs_Preview_Main_GetComments()
        {
            //UNDONE:Docs2: not implemented
            Assert.Inconclusive();
            SnTrace.Test.Write(">>>> ACT");
            /*<doc>*/
            /*</doc>*/
            SnTrace.Test.Write(">>>> ACT END");



            Assert.Inconclusive();
            // ACTION for doc
            var result = await RESTCaller.GetResponseJsonAsync(new ODataRequest
            {
                IsCollectionRequest = false,
                Path = "/Root/Content/IT/Document_Library/Calgary/BusinessPlan.docx",
                ActionName = "GetPreviewComments",
                Parameters = { { "page", "3" } }
            });
            //Console.WriteLine(result);

            // ASSERT
            Assert.Inconclusive();
        }

        /// <tab category="preview" article="previews" example="removeComment" />
        [TestMethod]
        [Description("Remove comment")]
        public async Task Docs_Preview_Main_RemoveComment()
        {
            //UNDONE:Docs2: not implemented
            Assert.Inconclusive();
            SnTrace.Test.Write(">>>> ACT");
            /*<doc>*/
            /*</doc>*/
            SnTrace.Test.Write(">>>> ACT END");



            Assert.Inconclusive();
            // ACTION for doc
            var body = @"models=[{
                ""id"": ""839ba802-d587-4153-b4e8-ccd4c593e1f4""}]";
            var result = await RESTCaller.GetResponseStringAsync(
                "/Root/Content/IT/Document_Library/Calgary/BusinessPlan.docx", "DeletePreviewComment",
                HttpMethod.Post, body);
            //Console.WriteLine(result);

            // ASSERT
            Assert.Inconclusive();
        }
    }
}
