﻿using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Client.TestsForDocs.Infrastructure;
using SenseNet.Diagnostics;

namespace SenseNet.Client.TestsForDocs
{
    [TestClass]
    public class Sharing : ClientIntegrationTestBase
    {
        /* ====================================================================================== Main */

        /// 
        [TestMethod]
        [Description("Share with a user")]
        public async Task Docs_Sharing_Main_Share()
        {
            //UNDONE:Docs2: not implemented
            Assert.Inconclusive();
            SnTrace.Test.Write(">>>> ACT");
            /*<doc>*/
            /*</doc>*/
            SnTrace.Test.Write(">>>> ACT END");


            /*
            Assert.Inconclusive();
            // ACTION for doc
            var body = @"models=[{
                ""token"": ""alba@sensenet.com"",
                ""level"": ""Open"",
                ""mode"": ""Private"",
                ""sendNotification"": true}]";
            var result = await RESTCaller.GetResponseStringAsync(
                "/Root/Content/IT", "Share", HttpMethod.Post, body);
            //Console.WriteLine(result);

            // ASSERT
            Assert.Inconclusive();
            */
        }

        /// 
        [TestMethod]
        [Description("Share content with external users via email")]
        public async Task Docs_Sharing_Main_ShareWithEmail()
        {
            //UNDONE:Docs2: not implemented
            Assert.Inconclusive();
            SnTrace.Test.Write(">>>> ACT");
            /*<doc>*/
            /*</doc>*/
            SnTrace.Test.Write(">>>> ACT END");


            /*
            Assert.Inconclusive();
            // ACTION for doc
            var body = @"models=[{
                ""token"": ""alba@sensenet.com"",
                ""level"": ""Open"",
                ""mode"": ""Public"",
                ""sendNotification"": true}]";
            var result = await RESTCaller.GetResponseStringAsync(
                "/Root/Content/IT", "Share", HttpMethod.Post, body);
            //Console.WriteLine(result);

            // ASSERT
            Assert.Inconclusive();
            */
        }

        /// 
        [TestMethod]
        [Description("Sharing levels")]
        public async Task Docs_Sharing_Main_Levels()
        {
            //UNDONE:Docs2: not implemented
            Assert.Inconclusive();
            SnTrace.Test.Write(">>>> ACT");
            /*<doc>*/
            /*</doc>*/
            SnTrace.Test.Write(">>>> ACT END");


            /*
            Assert.Inconclusive();
            // ACTION for doc
            var body = @"models=[{
                ""token"": ""alba@sensenet.com"",
                ""level"": ""Edit"",
                ""mode"": ""Private"",
                ""sendNotification"": true}]";
            var result = await RESTCaller.GetResponseStringAsync(
                "/Root/Content/IT", "Share", HttpMethod.Post, body);
            //Console.WriteLine(result);

            // ASSERT
            Assert.Inconclusive();
            */
        }

        /// 
        [TestMethod]
        [Description("Remove sharing")]
        public async Task Docs_Sharing_Main_Remove()
        {
            //UNDONE:Docs2: not implemented
            Assert.Inconclusive();
            SnTrace.Test.Write(">>>> ACT");
            /*<doc>*/
            /*</doc>*/
            SnTrace.Test.Write(">>>> ACT END");


            /*
            Assert.Inconclusive();
            // ACTION for doc
            var body = @"models=[{""id"": ""1b9abb5f-ed49-48c8-8edd-2c7e634bd77b""}]";
            var result = await RESTCaller.GetResponseStringAsync(
                "/Root/Content/IT", "RemoveSharing", HttpMethod.Post, body);
            //Console.WriteLine(result);

            // ASSERT
            Assert.Inconclusive();
            */
        }

        /// 
        [TestMethod]
        [Description("Getting sharing entries for a content")]
        public async Task Docs_Sharing_Main_GetSharing()
        {
            //UNDONE:Docs2: not implemented
            Assert.Inconclusive();
            SnTrace.Test.Write(">>>> ACT");
            /*<doc>*/
            /*</doc>*/
            SnTrace.Test.Write(">>>> ACT END");


            /*
            Assert.Inconclusive();
            // ACTION for doc
            var result = await RESTCaller.GetResponseStringAsync(
                "/Root/Content/IT", "GetSharing");
            //Console.WriteLine(result);

            // ASSERT
            Assert.Inconclusive();
            */
        }

        /// 
        [TestMethod]
        [Description("Content shared with a specific user")]
        public async Task Docs_Sharing_Main_GetSharedWith()
        {
            //UNDONE:Docs2: not implemented
            Assert.Inconclusive();
            SnTrace.Test.Write(">>>> ACT");
            /*<doc>*/
            /*</doc>*/
            SnTrace.Test.Write(">>>> ACT END");


            /*
            Assert.Inconclusive();
            // ACTION for doc
            var result = await Content.QueryAsync("SharedWith:@@CurrentUser@@");
            //foreach (dynamic content in result)
            //    Console.WriteLine($"{content.Id} {content.Name}");

            // ASSERT
            Assert.Inconclusive();
            */
        }

        /// 
        [TestMethod]
        [Description("Content shared by a specific user")]
        public async Task Docs_Sharing_Main_GetSharedBy()
        {
            //UNDONE:Docs2: not implemented
            Assert.Inconclusive();
            SnTrace.Test.Write(">>>> ACT");
            /*<doc>*/
            /*</doc>*/
            SnTrace.Test.Write(">>>> ACT END");


            /*
            Assert.Inconclusive();
            // ACTION for doc
            var result = await Content.QueryAsync("SharedBy:@@CurrentUser@@");
            //foreach (dynamic content in result)
            //    Console.WriteLine($"{content.Id} {content.Name}");

            // ASSERT
            Assert.Inconclusive();
            */
        }

        /// 
        [TestMethod]
        [Description("Notifications")]
        public async Task Docs_Sharing_Main_Notifications()
        {
            //UNDONE:Docs2: not implemented
            Assert.Inconclusive();
            SnTrace.Test.Write(">>>> ACT");
            /*<doc>*/
            /*</doc>*/
            SnTrace.Test.Write(">>>> ACT END");


            /*
            Assert.Inconclusive();
            // ACTION for doc
            var body = @"models=[{
                ""token"": ""alba@sensenet.com"",
                ""level"": ""Edit"",
                ""mode"": ""Private"",
                ""sendNotification"": false}]";
            var result = await RESTCaller.GetResponseStringAsync(
                "/Root/Content/IT", "Share", HttpMethod.Post, body);
            //Console.WriteLine(result);

            // ASSERT
            Assert.Inconclusive();
            */
        }
    }
}
