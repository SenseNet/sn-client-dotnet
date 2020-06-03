using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Tests.Accessors;

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
        public void QueryString_ParameterArray_AddWellKnown()
        {
            void ParamTest(string name, string value, string propertyName, object propertyValue)
            {
                var request = new ODataRequest(new ServerContext { Url = "https://example.com" })
                    { Path = "/Root/MyContent", IsCollectionRequest = true };

                request.Parameters.Add(name, value);

                var requestAcc = new ObjectAccessor(request);
                var propValue = requestAcc.GetProperty(propertyName);
                if (propertyValue is string[] stringArrayValue)
                {
                    propertyValue = string.Join(",", stringArrayValue);
                    propValue = string.Join(",", (string[])propValue);
                }

                Assert.AreEqual(0, request.Parameters.Count);
                Assert.AreEqual(propertyValue, propValue);
            }

            ParamTest("$top", "5", "Top", 5);
            ParamTest("$skip", "10", "Skip", 10);
            ParamTest("$expand", "a, b ,c/d", "Expand", new[] {"a", "b", "c/d"});
            ParamTest("$select", "a, b ,c/d", "Select", new[] { "a", "b", "c/d" });
            ParamTest("$filter", "isof(Folder)", "ChildrenFilter", "isof(Folder)");
            ParamTest("$orderby", "A Desc,B", "OrderBy", new[] { "A Desc", "B" });
            ParamTest("$inlinecount", "allpages", "InlineCount", InlineCountOptions.AllPages);
            ////ParamTest("$format", "value");
            //ParamTest("$count", "true");
            ParamTest("metadata", "no", "Metadata", MetadataFormat.None);
            ParamTest("metadata", "minimal", "Metadata", MetadataFormat.Minimal);
            ParamTest("enableautofilters", "true", "AutoFilters", FilterStatus.Enabled);
            ParamTest("enableautofilters", "false", "AutoFilters", FilterStatus.Disabled);
            ParamTest("enablelifespanfilter", "true", "LifespanFilter", FilterStatus.Enabled);
            ParamTest("enablelifespanfilter", "false", "LifespanFilter", FilterStatus.Disabled);
            ParamTest("version", "V1.0.P", "Version", "V1.0.P");
            ParamTest("scenario", "Scenario1", "Scenario", "Scenario1");
            ParamTest("query", "+A:a +B:b .COUNTONLY", "ContentQuery", "+A:a +B:b .COUNTONLY");
            ParamTest("permissions", "Open, Approve", "Permissions", new[] { "Open", "Approve" });
            ParamTest("user", "/root///user1", "User", "/root///user1");
        }
        [TestMethod]
        public void QueryString_ParameterArray_RemoveWellKnownByName()
        {
            void ParamTest(string name, string value, string propertyName, object propertyValue)
            {
                var request = new ODataRequest(new ServerContext { Url = "https://example.com" })
                { Path = "/Root/MyContent", IsCollectionRequest = true };

                request.Parameters.Add(name, value);

                var requestAcc = new ObjectAccessor(request);
                var propValue = requestAcc.GetProperty(propertyName);
                if (propertyValue is string[] stringArrayValue)
                {
                    propertyValue = string.Join(",", stringArrayValue);
                    propValue = string.Join(",", (string[])propValue);
                }

                Assert.AreEqual(0, request.Parameters.Count);
                Assert.AreEqual(propertyValue, propValue);

                request.Parameters.Remove(name);

                propValue = requestAcc.GetProperty(propertyName);

                object defaultValue = null;
                if (propertyValue is bool)
                    defaultValue = default(bool);
                if (propertyValue is int)
                    defaultValue = default(int);
                if (propertyValue is InlineCountOptions)
                    defaultValue = default(InlineCountOptions);
                if (propertyValue is MetadataFormat)
                    defaultValue = default(MetadataFormat);
                if (propertyValue is FilterStatus)
                    defaultValue = default(FilterStatus);

                Assert.AreEqual(0, request.Parameters.Count);
                Assert.AreEqual(defaultValue, propValue);
            }

            ParamTest("$top", "5", "Top", 5);
            ParamTest("$skip", "10", "Skip", 10);
            ParamTest("$expand", "a, b ,c/d", "Expand", new[] { "a", "b", "c/d" });
            ParamTest("$select", "a, b ,c/d", "Select", new[] { "a", "b", "c/d" });
            ParamTest("$filter", "isof(Folder)", "ChildrenFilter", "isof(Folder)");
            ParamTest("$orderby", "A Desc,B", "OrderBy", new[] { "A Desc", "B" });
            ParamTest("$inlinecount", "allpages", "InlineCount", InlineCountOptions.AllPages);
            ////ParamTest("$format", "value");
            //ParamTest("$count", "true");
            ParamTest("metadata", "no", "Metadata", MetadataFormat.None);
            ParamTest("metadata", "minimal", "Metadata", MetadataFormat.Minimal);
            ParamTest("enableautofilters", "true", "AutoFilters", FilterStatus.Enabled);
            ParamTest("enableautofilters", "false", "AutoFilters", FilterStatus.Disabled);
            ParamTest("enablelifespanfilter", "true", "LifespanFilter", FilterStatus.Enabled);
            ParamTest("enablelifespanfilter", "false", "LifespanFilter", FilterStatus.Disabled);
            ParamTest("version", "V1.0.P", "Version", "V1.0.P");
            ParamTest("scenario", "Scenario1", "Scenario", "Scenario1");
            ParamTest("query", "+A:a +B:b .COUNTONLY", "ContentQuery", "+A:a +B:b .COUNTONLY");
            ParamTest("permissions", "Open, Approve", "Permissions", new[] { "Open", "Approve" });
            ParamTest("user", "/root///user1", "User", "/root///user1");
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
            var expected = $"{ClientContext.Current.Server.Url}/OData.svc/('Root')/Action1?" +
                           $"metadata=minimal&$expand=Field1,Field2&$select=Id,Name,Field1,Field2&version=V16.78D";

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
            var expected = $"{ClientContext.Current.Server.Url}/OData.svc/content(79)/PropertyName?" +
                           $"metadata=minimal&$expand=Field1,Field2&$select=Id,Name,Field1,Field2&version=V16.78D";

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
            var expected = $"{ClientContext.Current.Server.Url}/OData.svc/Root?" +
                           $"metadata=no&$top=7&$skip=78&$select=Id,Name&$filter=isof('Folder')&" +
                           $"$orderby=Field1 desc,Field2,Field3 asc&$inlinecount=allpages&" +
                           $"enableautofilters=false&enablelifespanfilter=false&scenario=Scenario1";

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
            var expected = $"{ClientContext.Current.Server.Url}/OData.svc/Root/$count?" +
                           $"metadata=no&$filter=isof('Folder')&$inlinecount=allpages&" +
                           $"enableautofilters=false&enablelifespanfilter=false&scenario=Scenario1";

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
