using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SenseNet.Client.Tests
{
    [TestClass]
    public class FieldTests
    {
        [ClassInitialize]
        public static void ClassInitializer(TestContext _)
        {
            Initializer.InitializeServer();
        }

        [TestMethod]
        public async Task ReferenceField_LoadReferences()
        {
            var references = (await Content.LoadReferencesAsync("/Root", "CreatedBy")).ToArray();
            Assert.AreEqual(1, references.Single().Id);

            var single = await Content.LoadReferenceAsync("/Root", "CreatedBy");
            Assert.AreEqual(1, single.Id);
        }
    }
}
