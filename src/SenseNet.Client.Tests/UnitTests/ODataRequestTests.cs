using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Tests.Accessors;

namespace SenseNet.Client.Tests.UnitTests
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
        public void ODataRequest_NoServer_Error()
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
        public void ODataRequest_ServerWithoutUrl_Error()
        {
            using (new ServerSwindler())
            {
                ClientContext.Current.RemoveAllServers();

                // this will fail, because the server does not contain a url
                var _ = new ODataRequest(new ServerContext());
            }
        }
        [TestMethod]
        public void ODataRequest_ServerWithUrl()
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
        public void ODataRequest_QueryString_Parameters()
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
        public void ODataRequest_QueryString_ParameterArray()
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
        public void ODataRequest_QueryString_ParameterArray_AddWellKnown()
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
        public void ODataRequest_QueryString_ParameterArray_RemoveWellKnownByName()
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
        public void ODataRequest_QueryString_BuildFromProperties_EmptyIsBuggy()
        {
            var req = new ODataRequest();

            Assert.AreEqual("", req.ToString());
        }
        [TestMethod]
        public void ODataRequest_QueryString_BuildFromProperties_Defaults()
        {
            var expected = $"{ClientContext.Current.Server.Url}/OData.svc/('Root')?metadata=no";

            var req = new ODataRequest { Path = "/Root" };

            Assert.AreEqual(expected, req.ToString());
        }
        [TestMethod]
        public void ODataRequest_QueryString_BuildFromProperties_CollectionDefaults()
        {
            var expected = $"{ClientContext.Current.Server.Url}/OData.svc/Root?metadata=no";

            var req = new ODataRequest { Path = "/Root", IsCollectionRequest = true };

            Assert.AreEqual(expected, req.ToString());
        }
        [TestMethod]
        public void ODataRequest_QueryString_BuildFromProperties_PropertySet1()
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
                Expand = new[] { "Field1", "Field2" },
                Select = new[] { "Id", "Name", "Field1", "Field2" },
                Version = "V16.78D"
            };

            Assert.AreEqual(expected, req.ToString());
        }
        [TestMethod]
        public void ODataRequest_QueryString_BuildFromProperties_PropertySet2()
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
        public void ODataRequest_QueryString_BuildFromProperties_PropertySet3()
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
        public void ODataRequest_QueryString_BuildFromProperties_PropertySet4()
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

        /* ============================================================================= */

        private string _baseUri => ClientContext.Current.Server.Url;

        [TestMethod]
        public void OdataRequest_Id()
        {
            var request = new ODataRequest(null) { ContentId = 42 };
            Assert.AreEqual($"{_baseUri}/OData.svc/content(42)?metadata=no", request.ToString());
        }
        [TestMethod]
        public void OdataRequest_Path()
        {
            var request = new ODataRequest(null) { Path = "/Root/Content/MyFolder" };
            Assert.AreEqual($"{_baseUri}/OData.svc/Root/Content('MyFolder')?metadata=no", request.ToString());
        }
        [TestMethod]
        public void OdataRequest_CountOnly()
        {
            var request = new ODataRequest(null) { ContentId = 42, CountOnly = true };
            Assert.AreEqual($"{_baseUri}/OData.svc/content(42)/$count?metadata=no", request.ToString());
        }
        [TestMethod]
        public void OdataRequest_ActionName()
        {
            var request = new ODataRequest(null) { ContentId = 42, ActionName = "Action1" };
            Assert.AreEqual($"{_baseUri}/OData.svc/content(42)/Action1?metadata=no", request.ToString());
        }
        [TestMethod]
        public void OdataRequest_PropertyName()
        {
            var request = new ODataRequest(null) { ContentId = 42, PropertyName = "Property1" };
            Assert.AreEqual($"{_baseUri}/OData.svc/content(42)/Property1?metadata=no", request.ToString());
        }
        [TestMethod]
        public void OdataRequest_MetadataFull()
        {
            var request = new ODataRequest(null) { ContentId = 42, Metadata = MetadataFormat.Full };
            Assert.AreEqual($"{_baseUri}/OData.svc/content(42)", request.ToString());
        }
        [TestMethod]
        public void OdataRequest_MetadataMinimal()
        {
            var request = new ODataRequest(null) { ContentId = 42, Metadata = MetadataFormat.Minimal };
            Assert.AreEqual($"{_baseUri}/OData.svc/content(42)?metadata=minimal", request.ToString());
        }
        [TestMethod]
        public void OdataRequest_MetadataNo()
        {
            var request = new ODataRequest(null) { ContentId = 42, Metadata = MetadataFormat.None };
            Assert.AreEqual($"{_baseUri}/OData.svc/content(42)?metadata=no", request.ToString());
        }
        [TestMethod]
        public void OdataRequest_TopSkip()
        {
            var request = new ODataRequest(null) { ContentId = 42, Top = 10, Skip = 11 };
            Assert.AreEqual($"{_baseUri}/OData.svc/content(42)?metadata=no&$top=10&$skip=11", request.ToString());
        }
        [TestMethod]
        public void OdataRequest_ExpandSelect()
        {
            var request = new ODataRequest(null)
            {
                ContentId = 42,
                Expand = new[] { "Manager", "CreatedBy/Manager" },
                Select = new[] { "Id", "Name", "Manager/Name", "CreatedBy/Manager/Name" }
            };
            Assert.AreEqual($"{_baseUri}/OData.svc/content(42)?metadata=no&$expand=Manager,CreatedBy/Manager&" +
                            $"$select=Id,Name,Manager/Name,CreatedBy/Manager/Name", request.ToString());
        }
        [TestMethod]
        public void OdataRequest_Filter()
        {
            var request = new ODataRequest(null) { ContentId = 42, ChildrenFilter = "isof('Folder')" };
            Assert.AreEqual($"{_baseUri}/OData.svc/content(42)?metadata=no&$filter=isof('Folder')", request.ToString());
        }
        [TestMethod]
        public void OdataRequest_OrderBy()
        {
            var request = new ODataRequest(null) { ContentId = 42, OrderBy = new[] { "Name", "Index desc" } };
            Assert.AreEqual($"{_baseUri}/OData.svc/content(42)?metadata=no&$orderby=Name,Index desc", request.ToString());
        }
        [TestMethod]
        public void OdataRequest_InlineCountDefault()
        {
            var request = new ODataRequest(null) { ContentId = 42, InlineCount = InlineCountOptions.Default };
            Assert.AreEqual($"{_baseUri}/OData.svc/content(42)?metadata=no", request.ToString());
        }
        [TestMethod]
        public void OdataRequest_InlineCountNone()
        {
            var request = new ODataRequest(null) { ContentId = 42, InlineCount = InlineCountOptions.None };
            Assert.AreEqual($"{_baseUri}/OData.svc/content(42)?metadata=no", request.ToString());
        }
        [TestMethod]
        public void OdataRequest_InlineCountAllPages()
        {
            var request = new ODataRequest(null) { ContentId = 42, InlineCount = InlineCountOptions.AllPages };
            Assert.AreEqual($"{_baseUri}/OData.svc/content(42)?metadata=no&$inlinecount=allpages", request.ToString());
        }
        [TestMethod]
        public void OdataRequest_AutoFiltersDefault()
        {
            var request = new ODataRequest(null) { ContentId = 42, AutoFilters = FilterStatus.Default };
            Assert.AreEqual($"{_baseUri}/OData.svc/content(42)?metadata=no", request.ToString());
        }
        [TestMethod]
        public void OdataRequest_AutoFiltersEnabled()
        {
            var request = new ODataRequest(null) { ContentId = 42, AutoFilters = FilterStatus.Enabled };
            Assert.AreEqual($"{_baseUri}/OData.svc/content(42)?metadata=no&enableautofilters=true", request.ToString());
        }
        [TestMethod]
        public void OdataRequest_AutoFiltersDisabled()
        {
            var request = new ODataRequest(null) { ContentId = 42, AutoFilters = FilterStatus.Disabled };
            Assert.AreEqual($"{_baseUri}/OData.svc/content(42)?metadata=no&enableautofilters=false", request.ToString());
        }
        [TestMethod]
        public void OdataRequest_LifespanFilterDefault()
        {
            var request = new ODataRequest(null) { ContentId = 42, LifespanFilter = FilterStatus.Default };
            Assert.AreEqual($"{_baseUri}/OData.svc/content(42)?metadata=no", request.ToString());
        }
        [TestMethod]
        public void OdataRequest_LifespanFilterEnabled()
        {
            var request = new ODataRequest(null) { ContentId = 42, LifespanFilter = FilterStatus.Enabled };
            Assert.AreEqual($"{_baseUri}/OData.svc/content(42)?metadata=no&enablelifespanfilter=true", request.ToString());
        }
        [TestMethod]
        public void OdataRequest_LifespanFilterDisabled()
        {
            var request = new ODataRequest(null) { ContentId = 42, LifespanFilter = FilterStatus.Disabled };
            Assert.AreEqual($"{_baseUri}/OData.svc/content(42)?metadata=no&enablelifespanfilter=false", request.ToString());
        }
        [TestMethod]
        public void OdataRequest_Version()
        {
            var request = new ODataRequest(null) { ContentId = 42, Version = "V1.0.A" };
            Assert.AreEqual($"{_baseUri}/OData.svc/content(42)?metadata=no&version=V1.0.A", request.ToString());
        }
        [TestMethod]
        public void OdataRequest_Scenario()
        {
            var request = new ODataRequest(null) { ContentId = 42, Scenario = "scenario1" };
            Assert.AreEqual($"{_baseUri}/OData.svc/content(42)?metadata=no&scenario=scenario1", request.ToString());
        }
        [TestMethod]
        public void OdataRequest_ContentQuery()
        {
            var request = new ODataRequest(null) { ContentId = 42, ContentQuery = "Index:>100 .SORT:Name" };
            Assert.AreEqual($"{_baseUri}/OData.svc/content(42)?metadata=no&query=Index%3A%3E100%20.SORT%3AName",
                  request.ToString());
        }
        [TestMethod]
        public void OdataRequest_Permissions()
        {
            var request = new ODataRequest(null) { ContentId = 42, Permissions = new[] { "Save", "Custom01" } };
            Assert.AreEqual($"{_baseUri}/OData.svc/content(42)?metadata=no&permissions=Save,Custom01",
                request.ToString());
        }
        [TestMethod]
        public void OdataRequest_User()
        {
            var request = new ODataRequest(null) { ContentId = 42, User = "user1" };
            Assert.AreEqual($"{_baseUri}/OData.svc/content(42)?metadata=no&user=user1", request.ToString());
        }
        [TestMethod]
        public void OdataRequest_Parameters()
        {
            var request = new ODataRequest(null)
            {
                ContentId = 42,
                Parameters = { { "param1", "value1" }, { "param2", "value2" } }
            };
            Assert.AreEqual($"{_baseUri}/OData.svc/content(42)?metadata=no&param1=value1&param2=value2", request.ToString());
        }

        [TestMethod]
        public void OdataRequest_Error_MissingIdOrPath()
        {
            try
            {
                var request = new ODataRequest(null);
                var _ = request.ToString();
                Assert.Fail($"The expected {nameof(InvalidOperationException)} was not thrown.");
            }
            catch (InvalidOperationException e)
            {
                Assert.AreEqual("Invalid request properties: either content id or path must be provided.", e.Message);
            }
        }
        [TestMethod]
        public void OdataRequest_Error_ActionAndProperty()
        {
            try
            {
                var request = new ODataRequest(null)
                {
                    ContentId = 42,
                    ActionName = "action1",
                    PropertyName = "property1"
                };
                var _ = request.ToString();
                Assert.Fail($"The expected {nameof(InvalidOperationException)} was not thrown.");
            }
            catch (InvalidOperationException e)
            {
                Assert.AreEqual("Invalid request properties: both action name and property name are provided.",
                    e.Message);
            }
        }

    }
}
