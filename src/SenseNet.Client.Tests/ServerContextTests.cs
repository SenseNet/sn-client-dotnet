using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SenseNet.Client.Tests
{
    [TestClass]
    public class ServerContextTests
    {
        [TestMethod]
        public void AddSingleServer()
        {
            var serversToAdd = new[] { new ServerContext { Url = "1" } };

            ManipulateServers(serversToAdd, null, "1");
        }

        [TestMethod]
        public void AddMultipleServers()
        {
            var ids = Enumerable.Range(0, 10).ToArray();
            var serversToAdd = ids.Select(i => new ServerContext { Url = i.ToString() }).ToArray();

            ManipulateServers(serversToAdd, null, string.Join(";", ids));
        }

        [TestMethod]
        public void AddMultipleServers_Parallel()
        {
            var ids = Enumerable.Range(0, 500).ToArray();
            var serversToAdd = ids.Select(i => new ServerContext { Url = i.ToString() }).ToArray();

            var cc = new ClientContext();

            Parallel.ForEach(serversToAdd, s => cc.AddServer(s));

            // We have to sort the added servers before comparing to the original list, 
            // because Parallel.Foreach adds items in a mixed order.
            var actual = string.Join(";", cc.Servers.Select(s => Convert.ToInt32(s.Url)).OrderBy(sid => sid));

            Assert.AreEqual(string.Join(";", ids), actual);
        }

        [TestMethod]
        public void RemoveMultipleServers()
        {
            var idsToAdd = Enumerable.Range(0, 10).ToArray();
            
            var serversToAdd = idsToAdd.Select(i => new ServerContext { Url = i.ToString() }).ToArray();
            var serversToRemove = serversToAdd.Skip(5).ToArray();

            // remove the last few servers
            ManipulateServers(serversToAdd, serversToRemove, string.Join(";", idsToAdd.Take(5)));
            
            // try to remove servers by id: it should not work
            var idsToRemove = Enumerable.Range(5, 10).ToArray();
            serversToRemove = idsToRemove.Select(i => new ServerContext { Url = i.ToString() }).ToArray();

            // The expected list is the unmodified, full list, because 
            // add and remove compares servers as object reference!
            ManipulateServers(serversToAdd, serversToRemove, string.Join(";", idsToAdd));
        }

        private static void ManipulateServers(ServerContext[] serversToAdd, ServerContext[] serversToRemove, string expectedList)
        {
            var cc = new ClientContext();

            if (serversToAdd != null)
                cc.AddServers(serversToAdd);
            if (serversToRemove != null)
                cc.RemoveServers(serversToRemove);

            var actual = string.Join(";", cc.Servers.Select(s => s.Url));

            Assert.AreEqual(expectedList, actual);
        }
    }
}
