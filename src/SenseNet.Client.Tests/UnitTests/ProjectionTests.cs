using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SenseNet.Client.Tests.UnitTests
{
    [TestClass]
    public class ProjectionTests
    {
        [TestMethod]
        public void Projection_SelectAndExpand()
        {
            var projection = new Projection(new[] { "Id", "Name", "Members.Id", "Members.Name", "Members.Manager.Name", "Members.Manager.Avatar" });
            Assert.AreEqual("Id, Name, Members/Id, Members/Name, Members/Manager/Name, Members/Manager/Avatar", string.Join(", ", projection.Selection));
            Assert.AreEqual("Members, Members/Manager", string.Join(", ", projection.Expansion));
        }
        [TestMethod]
        public void Projection_SelectAndExpand_DotsAndSlashes()
        {
            var projection = new Projection(new[] { "Id", "Name", "Members/Id", "Members.Name", "Members.Manager/Name", "Members/Manager.Avatar" });
            Assert.AreEqual("Id, Name, Members/Id, Members/Name, Members/Manager/Name, Members/Manager/Avatar", string.Join(", ", projection.Selection));
            Assert.AreEqual("Members, Members/Manager", string.Join(", ", projection.Expansion));
        }
        [TestMethod]
        public void Projection_SelectKeepsFields_ExpandDistincts()
        {
            var projection = new Projection(new[] { "Id", "Members.Manager.Id", "Members.Id", "Name", "Id", "Members.Id", "Id" });
            Assert.AreEqual("Id, Members/Manager/Id, Members/Id, Name, Id, Members/Id, Id", string.Join(", ", projection.Selection));
            Assert.AreEqual("Members, Members/Manager", string.Join(", ", projection.Expansion));
        }
    }
}
