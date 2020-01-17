using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SenseNet.Client.Tests
{
    /// <summary>
    /// The tests in this class test the dynamic method call feature (TryInvokeMember) of the Content class.
    /// </summary>
    [TestClass]
    public class ActionTests
    {
        private static readonly string RootPath = "/Root/_ActionTests";
        private static readonly string AdminPath = "/Root/IMS/BuiltIn/Portal/Admin";
        private static readonly string VisitorPath = "/Root/IMS/BuiltIn/Portal/Visitor";

        [TestMethod]
        public async Task Dynamic_action_GET()
        {
            var folderName = Guid.NewGuid().ToString();
            var folderPath = RepositoryPath.Combine(RootPath, folderName);
            await Tools.EnsurePathAsync(folderPath).ConfigureAwait(false);

            dynamic folder = await Content.LoadAsync(folderPath).ConfigureAwait(false);

            // This 'method' does not exist locally. It will be resolved to an OData request 
            // and will return a task of type dynamic that will contain the result.
            Task<dynamic> task = folder.GetPermissionInfo(new {identity = AdminPath});
            var result = await task.ConfigureAwait(false);

            string resultPath = result.d.permissionInfo.path;

            Assert.AreEqual(folderPath, resultPath);
        }
        [TestMethod]
        public async Task Dynamic_action_POST()
        {
            var folderName = Guid.NewGuid().ToString();
            var folderPath = RepositoryPath.Combine(RootPath, folderName);
            await Tools.EnsurePathAsync(folderPath).ConfigureAwait(false);

            dynamic folder = await Content.LoadAsync(folderPath).ConfigureAwait(false);

            var haspermission = await folder.HasPermissionAsync(new[] { "See" }, VisitorPath).ConfigureAwait(false);
            Assert.IsFalse(haspermission, "Test prerequisite error: Visitor should not have this permission here.");

            // This 'method' does not exist locally. It will be resolved to an OData request 
            // and will return a task of type dynamic that will contain the result.
            Task<dynamic> task = folder.SetPermissions(HttpMethods.POST, new
            {
                r = new []
                {
                    new { identity = VisitorPath, See = "allow" }
                }
            });

            await task.ConfigureAwait(false);
            
            haspermission = await folder.HasPermissionAsync(new[] {"See"}, VisitorPath).ConfigureAwait(false);
            Assert.IsTrue(haspermission, "Visitor does not have the previously given permission.");
        }

        [ClassInitialize]
        public static void Cleanup(TestContext context)
        {
            Initializer.InitializeServer();

            var root = Content.LoadAsync(RootPath).Result;
            root?.DeleteAsync().Wait();
        }
    }
}
