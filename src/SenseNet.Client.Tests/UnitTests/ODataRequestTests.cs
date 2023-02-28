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
        #region ODataRequest_*
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
        public void OdataRequest_IdPath()
        {
            //UNDONE: Discussion: The path is irrelevant if the identifier is specified.
            var request = new ODataRequest(null) { ContentId = 42, Path = "/Root/Content/MyContent" };
            Assert.AreEqual($"{_baseUri}/OData.svc/content(42)?metadata=no", request.ToString());
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
        public void OdataRequest_Metadata()
        {
            var request = new ODataRequest(null) { ContentId = 42, Metadata = MetadataFormat.Full };
            Assert.AreEqual($"{_baseUri}/OData.svc/content(42)", request.ToString());

            request = new ODataRequest(null) { ContentId = 42, Metadata = MetadataFormat.Minimal };
            Assert.AreEqual($"{_baseUri}/OData.svc/content(42)?metadata=minimal", request.ToString());

            request = new ODataRequest(null) { ContentId = 42, Metadata = MetadataFormat.None };
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
        public void OdataRequest_InlineCount()
        {
            var request = new ODataRequest(null) { ContentId = 42, InlineCount = InlineCountOptions.Default };
            Assert.AreEqual($"{_baseUri}/OData.svc/content(42)?metadata=no", request.ToString());

            request = new ODataRequest(null) { ContentId = 42, InlineCount = InlineCountOptions.None };
            Assert.AreEqual($"{_baseUri}/OData.svc/content(42)?metadata=no", request.ToString());

            request = new ODataRequest(null) { ContentId = 42, InlineCount = InlineCountOptions.AllPages };
            Assert.AreEqual($"{_baseUri}/OData.svc/content(42)?metadata=no&$inlinecount=allpages", request.ToString());
        }
        [TestMethod]
        public void OdataRequest_AutoFilters()
        {
            var request = new ODataRequest(null) { ContentId = 42, AutoFilters = FilterStatus.Default };
            Assert.AreEqual($"{_baseUri}/OData.svc/content(42)?metadata=no", request.ToString());

            request = new ODataRequest(null) { ContentId = 42, AutoFilters = FilterStatus.Enabled };
            Assert.AreEqual($"{_baseUri}/OData.svc/content(42)?metadata=no&enableautofilters=true", request.ToString());

            request = new ODataRequest(null) { ContentId = 42, AutoFilters = FilterStatus.Disabled };
            Assert.AreEqual($"{_baseUri}/OData.svc/content(42)?metadata=no&enableautofilters=false", request.ToString());
        }
        [TestMethod]
        public void OdataRequest_LifespanFilter()
        {
            var request = new ODataRequest(null) { ContentId = 42, LifespanFilter = FilterStatus.Default };
            Assert.AreEqual($"{_baseUri}/OData.svc/content(42)?metadata=no", request.ToString());

            request = new ODataRequest(null) { ContentId = 42, LifespanFilter = FilterStatus.Enabled };
            Assert.AreEqual($"{_baseUri}/OData.svc/content(42)?metadata=no&enablelifespanfilter=true", request.ToString());

            request = new ODataRequest(null) { ContentId = 42, LifespanFilter = FilterStatus.Disabled };
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

        #endregion

        #region LoadContentRequest_*
        [TestMethod]
        public void LoadContentRequest_Id()
        {
            var request = new LoadContentRequest { ContentId = 42 };
            Assert.AreEqual($"{_baseUri}/OData.svc/content(42)?metadata=no",
                request.ToODataRequest(null).ToString());
        }
        [TestMethod]
        public void LoadContentRequest_Path()
        {
            var request = new LoadContentRequest { Path = "/Root/Content/MyContent" };
            Assert.AreEqual($"{_baseUri}/OData.svc/Root/Content('MyContent')?metadata=no",
                request.ToODataRequest(null).ToString());
        }
        [TestMethod]
        public void LoadContentRequest_Version()
        {
            var request = new ODataRequest(null) { ContentId = 42, Version = "V1.0.A" };
            Assert.AreEqual($"{_baseUri}/OData.svc/content(42)?metadata=no&version=V1.0.A", request.ToString());
        }
        [TestMethod]
        public void LoadContentRequest_ExpandSelect()
        {
            var request = new LoadContentRequest
            {
                ContentId = 42,
                Expand = new[] { "Manager", "CreatedBy/Manager" },
                Select = new[] { "Id", "Name", "Manager/Name", "CreatedBy/Manager/Name" }
            };
            Assert.AreEqual($"{_baseUri}/OData.svc/content(42)?metadata=no&$expand=Manager,CreatedBy/Manager&" +
                            $"$select=Id,Name,Manager/Name,CreatedBy/Manager/Name",
                request.ToODataRequest(null).ToString());

        }
        [TestMethod]
        public void LoadContentRequest_Metadata()
        {
            var request = new LoadContentRequest { ContentId = 42, Metadata = MetadataFormat.Full };
            Assert.AreEqual($"{_baseUri}/OData.svc/content(42)",
                request.ToODataRequest(null).ToString());

            request = new LoadContentRequest { ContentId = 42, Metadata = MetadataFormat.Minimal };
            Assert.AreEqual($"{_baseUri}/OData.svc/content(42)?metadata=minimal",
                request.ToODataRequest(null).ToString());

            request = new LoadContentRequest { ContentId = 42, Metadata = MetadataFormat.None };
            Assert.AreEqual($"{_baseUri}/OData.svc/content(42)?metadata=no",
                request.ToODataRequest(null).ToString());

        }
        [TestMethod]
        public void LoadContentRequest_Parameters()
        {
            var request = new LoadContentRequest
            {
                ContentId = 42,
                Parameters = { { "param1", "value1" }, { "param2", "value2" } }
            };
            Assert.AreEqual($"{_baseUri}/OData.svc/content(42)?metadata=no&param1=value1&param2=value2",
                request.ToODataRequest(null).ToString());

        }

        #endregion

        #region QueryContentRequest_*
        [TestMethod]
        public void QueryContentRequest_Version()
        {
            var request = new QueryContentRequest { Version = "V1.0.A" };
            Assert.AreEqual($"{_baseUri}/OData.svc/Root?metadata=no&version=V1.0.A", 
                request.ToODataRequest(null).ToString());
        }
        [TestMethod]
        public void QueryContentRequest_TopSkip()
        {
            var request = new QueryContentRequest { Top = 10, Skip = 11 };
            Assert.AreEqual($"{_baseUri}/OData.svc/Root?metadata=no&$top=10&$skip=11",
                request.ToODataRequest(null).ToString());
        }
        [TestMethod]
        public void QueryContentRequest_ExpandSelect()
        {
            var request = new QueryContentRequest
            {
                Expand = new[] { "Manager", "CreatedBy/Manager" },
                Select = new[] { "Id", "Name", "Manager/Name", "CreatedBy/Manager/Name" }
            };
            Assert.AreEqual($"{_baseUri}/OData.svc/Root?metadata=no&$expand=Manager,CreatedBy/Manager&" +
                            $"$select=Id,Name,Manager/Name,CreatedBy/Manager/Name",
                request.ToODataRequest(null).ToString());

        }
        [TestMethod]
        public void QueryContentRequest_Metadata()
        {
            var request = new QueryContentRequest { Metadata = MetadataFormat.Full };
            Assert.AreEqual($"{_baseUri}/OData.svc/Root",
                request.ToODataRequest(null).ToString());

            request = new QueryContentRequest { Metadata = MetadataFormat.Minimal };
            Assert.AreEqual($"{_baseUri}/OData.svc/Root?metadata=minimal",
                request.ToODataRequest(null).ToString());

            request = new QueryContentRequest { Metadata = MetadataFormat.None };
            Assert.AreEqual($"{_baseUri}/OData.svc/Root?metadata=no",
                request.ToODataRequest(null).ToString());

        }
        [TestMethod]
        public void QueryContentRequest_ContentQuery()
        {
            var request = new QueryContentRequest { ContentQuery = "Index:>100 .SORT:Name" };
            Assert.AreEqual($"{_baseUri}/OData.svc/Root?metadata=no&query=Index%3A%3E100%20.SORT%3AName",
                request.ToODataRequest(null).ToString());
        }
        [TestMethod]
        public void QueryContentRequest_Parameters()
        {
            var request = new QueryContentRequest
            {
                Parameters = { { "param1", "value1" }, { "param2", "value2" } }
            };
            Assert.AreEqual($"{_baseUri}/OData.svc/Root?metadata=no&param1=value1&param2=value2",
                request.ToODataRequest(null).ToString());

        }
        [TestMethod]
        public void QueryContentRequest_InlineCount()
        {
            var request = new QueryContentRequest { InlineCount = InlineCountOptions.Default };
            Assert.AreEqual($"{_baseUri}/OData.svc/Root?metadata=no",
                request.ToODataRequest(null).ToString());

            request = new QueryContentRequest { InlineCount = InlineCountOptions.None };
            Assert.AreEqual($"{_baseUri}/OData.svc/Root?metadata=no",
                request.ToODataRequest(null).ToString());

            request = new QueryContentRequest { InlineCount = InlineCountOptions.AllPages };
            Assert.AreEqual($"{_baseUri}/OData.svc/Root?metadata=no&$inlinecount=allpages",
                request.ToODataRequest(null).ToString());
        }
        [TestMethod]
        public void QueryContentRequest_AutoFilters()
        {
            var request = new QueryContentRequest { AutoFilters = FilterStatus.Default };
            Assert.AreEqual($"{_baseUri}/OData.svc/Root?metadata=no",
                request.ToODataRequest(null).ToString());

            request = new QueryContentRequest { AutoFilters = FilterStatus.Enabled };
            Assert.AreEqual($"{_baseUri}/OData.svc/Root?metadata=no&enableautofilters=true",
                request.ToODataRequest(null).ToString());

            request = new QueryContentRequest { AutoFilters = FilterStatus.Disabled };
            Assert.AreEqual($"{_baseUri}/OData.svc/Root?metadata=no&enableautofilters=false",
                request.ToODataRequest(null).ToString());
        }
        [TestMethod]
        public void QueryContentRequest_LifespanFilter()
        {
            var request = new QueryContentRequest { LifespanFilter = FilterStatus.Default };
            Assert.AreEqual($"{_baseUri}/OData.svc/Root?metadata=no",
                request.ToODataRequest(null).ToString());

            request = new QueryContentRequest { LifespanFilter = FilterStatus.Enabled };
            Assert.AreEqual($"{_baseUri}/OData.svc/Root?metadata=no&enablelifespanfilter=true",
                request.ToODataRequest(null).ToString());

            request = new QueryContentRequest { LifespanFilter = FilterStatus.Disabled };
            Assert.AreEqual($"{_baseUri}/OData.svc/Root?metadata=no&enablelifespanfilter=false",
                request.ToODataRequest(null).ToString());
        }
        [TestMethod]
        public void QueryContentRequest_OrderBy()
        {
            var request = new QueryContentRequest { OrderBy = new[] { "Name", "Index desc" } };
            Assert.AreEqual($"{_baseUri}/OData.svc/Root?metadata=no&$orderby=Name,Index desc",
                request.ToODataRequest(null).ToString());
        }

        #endregion

        #region LoadCollectionRequest_*
        [TestMethod]
        public void LoadCollectionRequest_MissingPath_Error()
        {
            try
            {
                var request = new LoadCollectionRequest();
                var _ = request.ToODataRequest(null).ToString();
                Assert.Fail($"Expected {nameof(InvalidOperationException)} was not thrown.");
            }
            catch (InvalidOperationException ex)
            {
                Assert.AreEqual("Invalid request properties: Path must be provided.", ex.Message);
            }
        }
        [TestMethod]
        public void LoadCollectionRequest_Version()
        {
            var request = new LoadCollectionRequest { Path = "/Root/MyContent", Version = "V1.0.A" };
            Assert.AreEqual($"{_baseUri}/OData.svc/Root/MyContent?metadata=no&version=V1.0.A",
                request.ToODataRequest(null).ToString());
        }
        [TestMethod]
        public void LoadCollectionRequest_TopSkip()
        {
            var request = new LoadCollectionRequest { Path = "/Root/MyContent", Top = 10, Skip = 11 };
            Assert.AreEqual($"{_baseUri}/OData.svc/Root/MyContent?metadata=no&$top=10&$skip=11",
                request.ToODataRequest(null).ToString());
        }
        [TestMethod]
        public void LoadCollectionRequest_ExpandSelect()
        {
            var request = new LoadCollectionRequest
            {
                Path = "/Root/MyContent",
                Expand = new[] { "Manager", "CreatedBy/Manager" },
                Select = new[] { "Id", "Name", "Manager/Name", "CreatedBy/Manager/Name" }
            };
            Assert.AreEqual($"{_baseUri}/OData.svc/Root/MyContent?metadata=no&$expand=Manager,CreatedBy/Manager&" +
                            $"$select=Id,Name,Manager/Name,CreatedBy/Manager/Name",
                request.ToODataRequest(null).ToString());

        }
        [TestMethod]
        public void LoadCollectionRequest_Metadata()
        {
            var request = new LoadCollectionRequest { Path = "/Root/MyContent", Metadata = MetadataFormat.Full };
            Assert.AreEqual($"{_baseUri}/OData.svc/Root/MyContent",
                request.ToODataRequest(null).ToString());

            request = new LoadCollectionRequest { Path = "/Root/MyContent", Metadata = MetadataFormat.Minimal };
            Assert.AreEqual($"{_baseUri}/OData.svc/Root/MyContent?metadata=minimal",
                request.ToODataRequest(null).ToString());

            request = new LoadCollectionRequest { Path = "/Root/MyContent", Metadata = MetadataFormat.None };
            Assert.AreEqual($"{_baseUri}/OData.svc/Root/MyContent?metadata=no",
                request.ToODataRequest(null).ToString());
        }
        [TestMethod]
        public void LoadCollectionRequest_ContentQuery()
        {
            var request = new LoadCollectionRequest { Path = "/Root/MyContent", ContentQuery = "Index:>100 .SORT:Name" };
            Assert.AreEqual($"{_baseUri}/OData.svc/Root/MyContent?metadata=no&query=Index%3A%3E100%20.SORT%3AName",
                request.ToODataRequest(null).ToString());
        }
        [TestMethod]
        public void LoadCollectionRequest_Parameters()
        {
            var request = new LoadCollectionRequest
            {
                Path = "/Root/MyContent",
                Parameters = { { "param1", "value1" }, { "param2", "value2" } }
            };
            Assert.AreEqual($"{_baseUri}/OData.svc/Root/MyContent?metadata=no&param1=value1&param2=value2",
                request.ToODataRequest(null).ToString());
        }
        [TestMethod]
        public void LoadCollectionRequest_InlineCount()
        {
            var request = new LoadCollectionRequest { Path = "/Root/MyContent", InlineCount = InlineCountOptions.Default };
            Assert.AreEqual($"{_baseUri}/OData.svc/Root/MyContent?metadata=no",
                request.ToODataRequest(null).ToString());

            request = new LoadCollectionRequest { Path = "/Root/MyContent", InlineCount = InlineCountOptions.None };
            Assert.AreEqual($"{_baseUri}/OData.svc/Root/MyContent?metadata=no",
                request.ToODataRequest(null).ToString());

            request = new LoadCollectionRequest { Path = "/Root/MyContent", InlineCount = InlineCountOptions.AllPages };
            Assert.AreEqual($"{_baseUri}/OData.svc/Root/MyContent?metadata=no&$inlinecount=allpages",
                request.ToODataRequest(null).ToString());
        }
        [TestMethod]
        public void LoadCollectionRequest_AutoFilters()
        {
            var request = new LoadCollectionRequest { Path = "/Root/MyContent", AutoFilters = FilterStatus.Default };
            Assert.AreEqual($"{_baseUri}/OData.svc/Root/MyContent?metadata=no",
                request.ToODataRequest(null).ToString());

            request = new LoadCollectionRequest { Path = "/Root/MyContent", AutoFilters = FilterStatus.Enabled };
            Assert.AreEqual($"{_baseUri}/OData.svc/Root/MyContent?metadata=no&enableautofilters=true",
                request.ToODataRequest(null).ToString());

            request = new LoadCollectionRequest { Path = "/Root/MyContent", AutoFilters = FilterStatus.Disabled };
            Assert.AreEqual($"{_baseUri}/OData.svc/Root/MyContent?metadata=no&enableautofilters=false",
                request.ToODataRequest(null).ToString());
        }
        [TestMethod]
        public void LoadCollectionRequest_LifespanFilter()
        {
            var request = new LoadCollectionRequest { Path = "/Root/MyContent", LifespanFilter = FilterStatus.Default };
            Assert.AreEqual($"{_baseUri}/OData.svc/Root/MyContent?metadata=no",
                request.ToODataRequest(null).ToString());

            request = new LoadCollectionRequest { Path = "/Root/MyContent", LifespanFilter = FilterStatus.Enabled };
            Assert.AreEqual($"{_baseUri}/OData.svc/Root/MyContent?metadata=no&enablelifespanfilter=true",
                request.ToODataRequest(null).ToString());

            request = new LoadCollectionRequest { Path = "/Root/MyContent", LifespanFilter = FilterStatus.Disabled };
            Assert.AreEqual($"{_baseUri}/OData.svc/Root/MyContent?metadata=no&enablelifespanfilter=false",
                request.ToODataRequest(null).ToString());
        }
        [TestMethod]
        public void LoadCollectionRequest_OrderBy()
        {
            var request = new LoadCollectionRequest { Path = "/Root/MyContent", OrderBy = new[] { "Name", "Index desc" } };
            Assert.AreEqual($"{_baseUri}/OData.svc/Root/MyContent?metadata=no&$orderby=Name,Index desc",
                request.ToODataRequest(null).ToString());
        }

        #endregion

        [TestMethod]
        public void LoadContentRequest_WellKnownParameters()
        {
            var request = new LoadContentRequest {ContentId = 42};
            request.Parameters.Add("version", "lastmajor");
            request.Parameters.Add("$select", "Id,Name,Owner/Name");
            request.Parameters.Add("$expand", "Owner");
            request.Parameters.Add("metadata", "minimal");
            request.Parameters.Add("$format", "verbosejson");

            var oDataRequest = request.ToODataRequest(null);

            Assert.AreEqual(42, oDataRequest.ContentId);
            Assert.AreEqual(false, oDataRequest.IsCollectionRequest);
            Assert.AreEqual(false, oDataRequest.CountOnly);

            Assert.AreEqual("lastmajor", request.Version);
            Assert.AreEqual("Id, Name, Owner/Name", string.Join(", ", request.Select));
            Assert.AreEqual("Owner", string.Join(", ", request.Expand));
            Assert.AreEqual(MetadataFormat.Minimal, request.Metadata);
            Assert.AreEqual(0, request.Parameters.Count);

            Assert.AreEqual($"{_baseUri}/OData.svc/content(42)?" +
                            $"metadata=minimal&" +
                            $"$expand=Owner&" +
                            $"$select=Id,Name,Owner/Name&" +
                            $"version=lastmajor&" +
                            $"$format=verbosejson",
                oDataRequest.ToString());
        }
        [TestMethod]
        public void LoadContentRequest_ForbiddenWellKnownParameters()
        {
            //UNDONE: Discussion: forbidden parameters should throw any exception.

            var request = new LoadContentRequest { ContentId = 42 };
            request.Parameters.Add("$top", "10");
            request.Parameters.Add("$skip", "15");

            var oDataRequest = request.ToODataRequest(null);

            Assert.AreEqual(42, oDataRequest.ContentId);

            Assert.AreEqual(2, request.Parameters.Count);

            Assert.AreEqual($"{_baseUri}/OData.svc/content(42)?metadata=no&$top=10&$skip=15",
                oDataRequest.ToString());
        }

        [TestMethod]
        public void QueryContentRequest_WellKnownParameters()
        {
            var request = new QueryContentRequest();
            request.Parameters.Add("version", "lastmajor");
            request.Parameters.Add("$top", "10");
            request.Parameters.Add("$skip", "11");
            request.Parameters.Add("$select", "Id,Name,Owner/Name");
            request.Parameters.Add("$expand", "Owner");
            request.Parameters.Add("metadata", "minimal");
            request.Parameters.Add("query", "TypeIs:Folder");
            request.Parameters.Add("$inlinecount", "allpages");
            request.Parameters.Add("enableautofilters", "true");
            request.Parameters.Add("enablelifespanfilter", "true");
            request.Parameters.Add("$orderby", "Name,Index desc");
            request.Parameters.Add("$format", "verbosejson");

            var oDataRequest = request.ToODataRequest(null);

            Assert.AreEqual("/Root", oDataRequest.Path);
            Assert.AreEqual(true, oDataRequest.IsCollectionRequest);
            Assert.AreEqual(false, oDataRequest.CountOnly);

            Assert.AreEqual("lastmajor", request.Version);
            Assert.AreEqual(10, request.Top);
            Assert.AreEqual(11, request.Skip);
            Assert.AreEqual("Id, Name, Owner/Name", string.Join(", ", request.Select));
            Assert.AreEqual("Owner", string.Join(", ", request.Expand));
            Assert.AreEqual(MetadataFormat.Minimal, request.Metadata);
            Assert.AreEqual(InlineCountOptions.AllPages, request.InlineCount);
            Assert.AreEqual(FilterStatus.Enabled, request.AutoFilters);
            Assert.AreEqual(FilterStatus.Enabled, request.LifespanFilter);
            Assert.AreEqual("Name, Index desc", string.Join(", ", request.OrderBy));
            Assert.AreEqual("TypeIs:Folder", request.ContentQuery);
            Assert.AreEqual(0, request.Parameters.Count);

            Assert.AreEqual($"{_baseUri}/OData.svc/Root?" +
                            $"metadata=minimal&" +
                            $"$top=10&" +
                            $"$skip=11&" +
                            $"$expand=Owner&" +
                            $"$select=Id,Name,Owner/Name&" +
                            $"$orderby=Name,Index desc&" +
                            $"$inlinecount=allpages&" +
                            $"enableautofilters=true&" +
                            $"enablelifespanfilter=true&" +
                            $"version=lastmajor&" +
                            $"query=TypeIs%3AFolder&" +
                            $"$format=verbosejson",
                oDataRequest.ToString());
        }

        [TestMethod]
        public void LoadCollectionRequest_WellKnownParameters()
        {
            var request = new LoadCollectionRequest { Path = "/Root/MyContent" };
            request.Parameters.Add("version", "lastmajor");
            request.Parameters.Add("$top", "10");
            request.Parameters.Add("$skip", "11");
            request.Parameters.Add("$select", "Id,Name,Owner/Name");
            request.Parameters.Add("$expand", "Owner");
            request.Parameters.Add("metadata", "minimal");
            request.Parameters.Add("query", "TypeIs:Folder");
            request.Parameters.Add("$inlinecount", "allpages");
            request.Parameters.Add("$filter", "isof('Folder')");
            request.Parameters.Add("enableautofilters", "true");
            request.Parameters.Add("enablelifespanfilter", "true");
            request.Parameters.Add("$orderby", "Name,Index desc");
            request.Parameters.Add("$format", "verbosejson");

            var oDataRequest = request.ToODataRequest(null);

            Assert.AreEqual("/Root/MyContent", oDataRequest.Path);
            Assert.AreEqual(true, oDataRequest.IsCollectionRequest);
            Assert.AreEqual(false, oDataRequest.CountOnly);

            Assert.AreEqual("/Root/MyContent", request.Path);
            Assert.AreEqual("lastmajor", request.Version);
            Assert.AreEqual(10, request.Top);
            Assert.AreEqual(11, request.Skip);
            Assert.AreEqual("Id, Name, Owner/Name", string.Join(", ", request.Select));
            Assert.AreEqual("Owner", string.Join(", ", request.Expand));
            Assert.AreEqual(MetadataFormat.Minimal, request.Metadata);
            Assert.AreEqual(InlineCountOptions.AllPages, request.InlineCount);
            Assert.AreEqual("isof('Folder')", request.ChildrenFilter);
            Assert.AreEqual(FilterStatus.Enabled, request.AutoFilters);
            Assert.AreEqual(FilterStatus.Enabled, request.LifespanFilter);
            Assert.AreEqual("Name, Index desc", string.Join(", ", request.OrderBy));
            Assert.AreEqual("TypeIs:Folder", request.ContentQuery);
            Assert.AreEqual(0, request.Parameters.Count);

            Assert.AreEqual($"{_baseUri}/OData.svc/Root/MyContent?" +
                            $"metadata=minimal&" +
                            $"$top=10&" +
                            $"$skip=11&" +
                            $"$expand=Owner&" +
                            $"$select=Id,Name,Owner/Name&" +
                            $"$filter=isof('Folder')&" +
                            $"$orderby=Name,Index desc&" +
                            $"$inlinecount=allpages&" +
                            $"enableautofilters=true&" +
                            $"enablelifespanfilter=true&" +
                            $"version=lastmajor&" +
                            $"query=TypeIs%3AFolder&" +
                            $"$format=verbosejson",
                request.ToODataRequest(null).ToString());
        }
    }
}
