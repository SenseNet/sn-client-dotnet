using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SenseNet.Client.Tests
{
    internal class ServerSwindler : IDisposable
    {
        private readonly ServerContext[] _original;
        public ServerSwindler()
        {
            _original = ClientContext.Current.Servers;
        }

        public void Dispose()
        {
            ClientContext.Current.RemoveAllServers();
            ClientContext.Current.AddServers(_original);
        }
    }

    [TestClass]
    public class ODataRequestTests
    {
        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void NoServer_Error()
        {
            using (new ServerSwindler())
            {
                ClientContext.Current.RemoveAllServers();

                // this will fail, because there is no server configured or provided
                var _ = new ODataRequest();
            }
        }
        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void ServerWithoutUrl_Error()
        {
            using (new ServerSwindler())
            {
                ClientContext.Current.RemoveAllServers();

                // this will fail, because the server does not contain a url
                var _ = new ODataRequest(new ServerContext());
            }
        }
        [TestMethod]
        public void ServerWithUrl()
        {
            using (new ServerSwindler())
            {
                ClientContext.Current.RemoveAllServers();

                // this should work: the url is defined
                var _ = new ODataRequest(new ServerContext
                {
                    Url = "example.com"
                });
            }
        }

        [TestMethod]
        public void QueryString_Parameters()
        {
            var request = new ODataRequest(new ServerContext
            {
                Url = "https://example.com",
            })
            {
                Path = "/Root/MyContent",
                IsCollectionRequest = false
            };

            request.Parameters.Add("Id", "1");
            request.Parameters.Add("Name", "Value");

            var expected = "https://example.com/OData.svc/Root('MyContent')?metadata=no&Id=1&Name=Value";

            // ACTION
            var actual = request.ToString();

            // ASSERT
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void QueryString_ParameterArray()
        {
            var request = new ODataRequest(new ServerContext
            {
                Url = "https://example.com",
            })
            {
                Path = "/Root/MyContent",
                IsCollectionRequest = false
            };

            request.Parameters.Add("Id", "1");
            request.Parameters.Add("Id", "2");
            request.Parameters.Add("Id", "3");
            request.Parameters.Add("Name", "Value");

            var expected = "https://example.com/OData.svc/Root('MyContent')?metadata=no&Id=1&Id=2&Id=3&Name=Value";

            // ACTION
            var actual = request.ToString();

            // ASSERT
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void QueryString_ParameterArray_AddInvalid()
        {
            void ParamTest(string name, string value)
            {
                try
                {
                    var request = new ODataRequest(new ServerContext { Url = "https://example.com" })
                        { Path = "/Root/MyContent", IsCollectionRequest = true };

                    request.Parameters.Add(name, value);
                    Assert.Fail("The expected InvalidOperationException was not thrown.");
                }
                catch (InvalidOperationException) { /* ignored */ }
            }

            ParamTest("$top", "value");
            ParamTest("$skip", "value");
            ParamTest("$expand", "value");
            ParamTest("$select", "value");
            ParamTest("$filter", "value");
            ParamTest("$orderby", "value");
            ParamTest("$inlinecount", "value");
            //ParamTest("$format", "value");
            ParamTest("$count", "value");
            ParamTest("metadata", "value");
            ParamTest("enableautofilters", "value");
            ParamTest("enablelifespanfilter", "value");
            ParamTest("version", "value");
            ParamTest("scenario", "value");

            ParamTest("query", "value");
            ParamTest("permissions", "value");
            ParamTest("user", "value");
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void QueryString_BuildFromProperties_EmptyIsBuggy()
        {
            var req = new ODataRequest();

            Assert.AreEqual("", req.ToString());
        }
        [TestMethod]
        public void QueryString_BuildFromProperties_Defaults()
        {
            var expected = $"{ClientContext.Current.Server.Url}/OData.svc/('Root')?metadata=no";

            var req = new ODataRequest { Path = "/Root" };

            Assert.AreEqual(expected, req.ToString());
        }
        [TestMethod]
        public void QueryString_BuildFromProperties_CollectionDefaults()
        {
            var expected = $"{ClientContext.Current.Server.Url}/OData.svc/Root?metadata=no";

            var req = new ODataRequest { Path = "/Root", IsCollectionRequest = true };

            Assert.AreEqual(expected, req.ToString());
        }
        [TestMethod]
        public void QueryString_BuildFromProperties_PropertySet1()
        {
            var expected = $"{ClientContext.Current.Server.Url}/OData.svc/('Root')/Action1?version=V16.78D&" +
                           $"$select=Id,Name,Field1,Field2&$expand=Field1,Field2&metadata=minimal";

            var req = new ODataRequest
            {
                Path = "/Root",
                //ContentId = 79,
                IsCollectionRequest = false,
                Metadata = MetadataFormat.Minimal,
                ActionName = "Action1",
                //PropertyName = "PropertyName",
                CountOnly = false,
                Expand = new[] {"Field1", "Field2"},
                Select = new []{ "Id" , "Name" , "Field1" , "Field2" },
                Version = "V16.78D"
            };

            Assert.AreEqual(expected, req.ToString());
        }
        [TestMethod]
        public void QueryString_BuildFromProperties_PropertySet2()
        {
            var expected = $"{ClientContext.Current.Server.Url}/OData.svc/content(79)/PropertyName?version=V16.78D&" +
                           $"$select=Id,Name,Field1,Field2&$expand=Field1,Field2&metadata=minimal";

            var req = new ODataRequest
            {
                //Path = "/Root",
                ContentId = 79,
                IsCollectionRequest = false,
                Metadata = MetadataFormat.Minimal,
                //ActionName = "Action1",
                PropertyName = "PropertyName",
                CountOnly = false,
                Expand = new[] { "Field1", "Field2" },
                Select = new[] { "Id", "Name", "Field1", "Field2" },
                Version = "V16.78D",
            };

            Assert.AreEqual(expected, req.ToString());
        }
        [TestMethod]
        public void QueryString_BuildFromProperties_PropertySet3()
        {
            var expected = $"{ClientContext.Current.Server.Url}/OData.svc/Root?$top=7&$skip=78&" +
                           $"$select=Id,Name&metadata=no&$inlinecount=allpages&$filter=isof('Folder')" +
                           $"&enableautofilters=false&enablelifespanfilter=false&scenario=Scenario1&" +
                           $"$orderby=Field1 desc,Field2,Field3 asc";

            var req = new ODataRequest
            {
                Path = "/Root",
                //ContentId = 79,
                IsCollectionRequest = true,
                Metadata = MetadataFormat.None,
                //ActionName = "Action1",
                //PropertyName = "PropertyName",
                CountOnly = false,
                AutoFilters = FilterStatus.Disabled,
                LifespanFilter = FilterStatus.Disabled,
                ChildrenFilter = "isof('Folder')",
                InlineCount = InlineCountOptions.AllPages,
                OrderBy = new[] { "Field1 desc", "Field2", "Field3 asc" },
                Scenario = "Scenario1",
                Select = new[] { "Id", "Name" },
                Top = 7,
                Skip = 78,
            };

            Assert.AreEqual(expected, req.ToString());
        }
        [TestMethod]
        public void QueryString_BuildFromProperties_PropertySet4()
        {
            var expected = $"{ClientContext.Current.Server.Url}/OData.svc/Root/$count?metadata=no&" +
                           $"$inlinecount=allpages&$filter=isof('Folder')&enableautofilters=false&" +
                           $"enablelifespanfilter=false&scenario=Scenario1";

            var req = new ODataRequest
            {
                Path = "/Root",
                //ContentId = 79,
                IsCollectionRequest = true,
                //Metadata = MetadataFormat.None,
                //ActionName = "Action1",
                //PropertyName = "PropertyName",
                CountOnly = true,
                AutoFilters = FilterStatus.Disabled,
                LifespanFilter = FilterStatus.Disabled,
                ChildrenFilter = "isof('Folder')",
                InlineCount = InlineCountOptions.AllPages,
                Scenario = "Scenario1",
            };

            Assert.AreEqual(expected, req.ToString());
        }
    }
}
