using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Client.Linq;
using System.Reflection;
using System.ServiceProcess;
using SenseNet.Extensions.DependencyInjection;
using SenseNet.Testing;

namespace SenseNet.Client.Tests.UnitTests;

[TestClass]
public class LinqTests : TestBase
{

    [TestMethod]
    public async Task Linq_AsQueryable()
    {
        await LinqTest(repository =>
        {
            Assert.IsTrue(repository.Content is IQueryable<Content>);

            var allContent = repository.Content;
            var allContent2 = repository.Content;
            Assert.AreNotSame(allContent, allContent2);

            var asQueryable = allContent.AsQueryable();
            Assert.AreSame(allContent, asQueryable);
        });
    }

    [TestMethod]
    public async Task Linq_IdEquality()
    {
        await LinqTest(repository =>
        {
            var expected = "Id:42";
            Assert.AreEqual(expected, GetQueryString(repository.Content.Where(c => c.Id == 42)));
            Assert.AreEqual(expected, GetQueryString(from c in repository.Content where c.Id == 42 select c));
        });
    }

    [TestMethod]
    public async Task Linq_IdRange_Order()
    {
        await LinqTest(repository =>
        {
            var expected = "Id:<4 .SORT:Id";
            Assert.AreEqual(expected, GetQueryString(repository.Content.Where(c => c.Id < 4).OrderBy(c => c.Id)));
            Assert.AreEqual(expected, GetQueryString(from c in repository.Content where c.Id < 4 orderby c.Id select c));

            expected = "Id:<=4 .SORT:Id";
            Assert.AreEqual(expected, GetQueryString(repository.Content.Where(c => c.Id <= 4).OrderBy(c => c.Id)));
            Assert.AreEqual(expected, GetQueryString(from c in repository.Content where c.Id <= 4 orderby c.Id select c));

            expected = "Id:<=4 .REVERSESORT:Id";
            Assert.AreEqual(expected, GetQueryString(repository.Content.Where(c => c.Id <= 4).OrderByDescending(c => c.Id)));
            Assert.AreEqual(expected, GetQueryString(from c in repository.Content where c.Id <= 4 orderby c.Id descending select c));

            expected = "+Id:>1 +Id:<=4 .SORT:Id";
            Assert.AreEqual(expected, GetQueryString(repository.Content.Where(c => c.Id <= 4 && c.Id > 1).OrderBy(c => c.Id)));
            Assert.AreEqual(expected, GetQueryString(from c in repository.Content where c.Id <= 4 && c.Id > 1 orderby c.Id select c));
        });
    }

    [TestMethod]
    public async Task Linq_SingleNegativeTerm()
    {
        await LinqTest(repository =>
        {
            Assert.AreEqual("-Id:42 +Id:>0", GetQueryString(repository.Content.Where(c => c.Id != 42)));
        });
    }
    [TestMethod]
    public async Task Linq_StartsWithEndsWithContains()
    {
        await LinqTest(repository =>
        {
            Assert.AreEqual("Name:car*", GetQueryString(repository.Content.Where(c => c.Name.StartsWith("Car"))));
            Assert.AreEqual("Name:*r2", GetQueryString(repository.Content.Where(c => c.Name.EndsWith("r2"))));
            Assert.AreEqual("Name:*ro*", GetQueryString(repository.Content.Where(c => c.Name.Contains("ro"))));
        });
    }

    [TestMethod]
    public async Task Linq_CaseSensitivity()
    {
        await LinqTest(repository =>
        {
            var expected = "Name:admin .SORT:Id";
            Assert.AreEqual(expected, GetQueryString(repository.Content.Where(c => c.Name == "admin").OrderBy(c => c.Id)));
            Assert.AreEqual(expected, GetQueryString(from c in repository.Content where c.Name == "admin" orderby c.Id select c));

            expected = "Name:admin .SORT:Id";
            Assert.AreEqual(expected, GetQueryString(repository.Content.Where(c => c.Name == "Admin").OrderBy(c => c.Id)));
            Assert.AreEqual(expected, GetQueryString(from c in repository.Content where c.Name == "Admin" orderby c.Id select c));
        });
    }

    [TestMethod]
    public async Task Linq_EmptyString()
    {
        await LinqTest(repository =>
        {
            Assert.AreEqual("DisplayName:''", GetQueryString(repository.Content.Where(c => c.DisplayName == "")));
        });
    }
    [TestMethod]
    public async Task Linq_NullString()
    {
        await LinqTest(repository =>
        {
            var expected = "DisplayName:''";
            Assert.AreEqual(expected, GetQueryString(repository.Content.Where(c => (string)c["DisplayName"] == null)));
            Assert.AreEqual(expected, GetQueryString(from c in repository.Content where (string)c["DisplayName"] == null select c));
        });
    }
    [TestMethod]
    public async Task Linq_DateTime()
    {
        await LinqTest(repository =>
        {
            var d0 = DateTime.UtcNow.AddDays(-2);

            // ModificationDate:<'2345-06-07 08:09:10.0000'
            var q1 = GetQueryString(repository.Content.Where(c => c.ModificationDate < DateTime.UtcNow.AddDays(-2)));
            q1 = q1.Substring(19, 24);
            var d1 = DateTime.Parse(q1);

            Assert.IsTrue(d1 - d0 < TimeSpan.FromSeconds(1));

            var q2 = GetQueryString(from c in repository.Content where c.ModificationDate < DateTime.UtcNow.AddDays(-2) select c);
            q2 = q2.Substring(19, 24);
            var d2 = DateTime.Parse(q2);

            Assert.IsTrue(d2 - d0 < TimeSpan.FromSeconds(1));
        });
    }

    [TestMethod]
    public async Task Linq_NegativeTerm()
    {
        await LinqTest(repository =>
        {
            Assert.AreEqual("-Id:2 +Id:<=4",
                GetQueryString(repository.Content.Where(c => c.Id <= 4 && c.Id != 2)));
            Assert.AreEqual("-Id:2 +Id:>0",
                GetQueryString(repository.Content.Where(c => c.Id != 2)));
            Assert.AreEqual("-Id:2 +Id:>0",
                GetQueryString(repository.Content.Where(c => c.Id > 0 && c.Id != 2)));
        });
    }

    [TestMethod]
    public async Task Linq_Bool()
    {
        await LinqTest(repository =>
        {
            var q = GetQueryString(repository.Content.Where(c => c.IsFolder == true));
            Assert.AreEqual("IsFolder:yes", q);

            q = GetQueryString(repository.Content.Where(c => c.IsFolder == false));
            Assert.AreEqual("IsFolder:no", q);

            q = GetQueryString(repository.Content.Where(c => c.IsFolder != true));
            Assert.AreEqual("IsFolder:no", q);

            q = GetQueryString(repository.Content.Where(c => true == c.IsFolder));
            Assert.AreEqual("IsFolder:yes", q);

            q = GetQueryString(repository.Content.Where(c => (bool)c["Hidden"]));
            Assert.AreEqual("Hidden:yes", q);

            q = GetQueryString(repository.Content.OfType<Workspace>().Where(c => c.IsWallContainer == true));
            Assert.AreEqual("+TypeIs:workspace +IsWallContainer:yes", q);
        });
    }
    [TestMethod]
    public async Task Linq_Bool_Negation()
    {
        await LinqTest(repository =>
        {
            var q = GetQueryString(repository.Content.Where(c => !(c.IsFolder == true)));
            Assert.AreEqual("IsFolder:no", q);

            q = GetQueryString(repository.Content.Where(c => c.IsFolder != true));
            Assert.AreEqual("IsFolder:no", q);

            // ReSharper disable once NegativeEqualityExpression
            q = GetQueryString(repository.Content.Where(c => !(c.IsFolder == true)));
            Assert.AreEqual("IsFolder:no", q);

            q = GetQueryString(repository.Content.Where(c => !(bool)c["Hidden"]));
            Assert.AreEqual("Hidden:no", q);

            q = GetQueryString(repository.Content.OfType<Workspace>().Where(c => !(c.IsWallContainer == true)));
            Assert.AreEqual("+TypeIs:workspace +IsWallContainer:no", q);

        });
    }
    [TestMethod]
    public async Task Linq_Negation()
    {
        await LinqTest(repository =>
        {
            var q = GetQueryString(repository.Content.Where(c => c.Index != 42));
            Assert.AreEqual("-Index:42 +Id:>0", q);

            // ReSharper disable once NegativeEqualityExpression
            q = GetQueryString(repository.Content.Where(c => !(c.Index == 42)));
            Assert.AreEqual("-Index:42 +Id:>0", q);

            // ReSharper disable once NegativeEqualityExpression
            q = GetQueryString(repository.Content.Where(c => !(!(c.Index == 42) && c.IsFolder == true)));
            Assert.AreEqual("-(+IsFolder:yes -Index:42) +Id:>0", q);
        });
    }

    #region RefTestNode
    private class RefTestNode : Folder
    {
        public RefTestNode(IRestCaller restCaller, ILogger<Content> logger) : base(restCaller, logger) { }

        public RefTestNode Wife { get; set; }
        public RefTestNode Husband { get; set; }
        public RefTestNode Mother { get; set; }
        public RefTestNode Father { get; set; }
        public RefTestNode Daughter { get; set; }
        public RefTestNode Son { get; set; }
        public RefTestNode Sister { get; set; }
        public RefTestNode Brother { get; set; }
        public string NickName { get; set; }
        public int Age { get; set; }
        public IEnumerable<Content> Neighbors { get; set; }
    }
    #endregion

    [TestMethod]
    public async Task Linq_SingleReference()
    {
        await LinqTest(repository =>
        {
            var mother1 = repository.CreateContent("/Root/Content", "RefTestNode", "Mother1");
            mother1.Id = 42;

            Assert.AreEqual(
                $"+TypeIs:reftestnode +Mother:{mother1.Id}",
                GetQueryString(repository.Content.OfType<RefTestNode>().Where(c => c.Mother == mother1)));
        });
    }
    [TestMethod]
    public async Task Linq_MultiReference()
    {
        await LinqTest(repository =>
        {
            var neighbor1 = repository.CreateContent("/Root/Content", "RefTestNode", "Neighbor1");
            Assert.AreEqual(
                $"Neighbors:{neighbor1.Id}",
                GetQueryString(repository.Content.Where(c => ((RefTestNode)c).Neighbors.Contains(neighbor1))));
        });
    }

    //    //[TestMethod]
    //    //public async Task Linq_EmptyReference()
    //    //{
    //    //    Content[] result;
    //    //    string expected, actual;
    //    //    QueryResult qresult;

    //    //    var mother1 = Node.LoadNode(TestRoot2.Path + "/Mother1");
    //    //    var mother2 = Node.LoadNode(TestRoot2.Path + "/Mother2");
    //    //    var child1 = Node.LoadNode(TestRoot2.Path + "/Child1");
    //    //    var child2 = Node.LoadNode(TestRoot2.Path + "/Child2");
    //    //    var child3 = Node.LoadNode(TestRoot2.Path + "/Child3");

    //    //    qresult = ContentQuery.Query(String.Concat("+Mother:null +InTree:", TestRoot2.Path, " .AUTOFILTERS:OFF"));
    //    //    result = repository.Content.DisableAutofilters().Where(c => c.InTree(TestRoot2) && ((RefTestNode)c.ContentHandler).Mother == null).OrderBy(c => c.Name).ToArray();
    //    //    Assert.IsTrue(result.Length == 3, String.Format("#5: count is {0}, expected: 3", result.Length));
    //    //    expected = String.Concat(child3.Id, ", ", mother1.Id, ", ", mother2.Id);
    //    //    actual = String.Join(", ", result.Select(x => x.Id));
    //    //    Assert.IsTrue(expected == actual, String.Format("#6: actual is {0}, expected: {1}", actual, expected));

    //    //    qresult = ContentQuery.Query(String.Concat("-Mother:null +InTree:", TestRoot2.Path, " +TypeIs:reftestnode .AUTOFILTERS:OFF"));
    //    //    result = repository.Content.DisableAutofilters().Where(c => c.InTree(TestRoot2) && ((RefTestNode)c.ContentHandler).Mother != null && c.ContentHandler is RefTestNode).OrderBy(c => c.Name).ToArray();
    //    //    Assert.IsTrue(result.Length == 2, String.Format("#5: count is {0}, expected: 2", result.Length));
    //    //    expected = String.Concat(child1.Id, ", ", child2.Id);
    //    //    actual = String.Join(", ", result.Select(x => x.Id));
    //    //    Assert.IsTrue(expected == actual, String.Format("#6: actual is {0}, expected: {1}", actual, expected));
    //    //}

    //    //[TestMethod]
    //    //public async Task Linq_Children()
    //    //{
    //    //    var folderName = "Linq_Children_test";
    //    //    var folder = Folder.Load<Folder>(RepositoryPath.Combine(TestRoot.Path, folderName));
    //    //    if (folder == null)
    //    //    {
    //    //        folder = new Folder(TestRoot) { Name = folderName };
    //    //        folder.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();
    //    //        for (int i = 0; i < 4; i++)
    //    //        {
    //    //            var content = Content.CreateNew("Car", folder, "Car" + i);
    //    //            content.ContentHandler.Index = i;
    //    //            content.Save();
    //    //        }
    //    //    }
    //    //    var folderContent = Content.Create(folder);

    //    //    var enumerable = folderContent.Children.DisableAutofilters().Where(c => c.Index < 2).OrderBy(c => c.Name);
    //    //    var result = enumerable.ToArray();

    //    //    var paths = result.Select(c => c.Path).ToArray();

    //    //    Assert.IsTrue(result.Length == 2, String.Format("result.Length is {0}, expected: 2.", result.Length));
    //    //    Assert.IsTrue(result[0].Name == "Car0", String.Format("result[0].Name is {0}, expected: 'Car0'.", result[0].Name));
    //    //    Assert.IsTrue(result[1].Name == "Car1", String.Format("result[1].Name is {0}, expected: 'Car1'.", result[1].Name));

    //    //}

    //    //[TestMethod]
    //    //public async Task Linq_Children_Count()
    //    //{
    //    //    if (ContentQuery.Query(".AUTOFILTERS:OFF .COUNTONLY Infolder:" + TestRoot.Path).Count == 0)
    //    //        for (int i = 0; i < 3; i++)
    //    //            Content.CreateNew("Car", TestRoot, Guid.NewGuid().ToString()).Save();
    //    //    var r = ContentQuery.Query(".AUTOFILTERS:OFF InFolder:" + TestRoot.Path);
    //    //    var expected = r.Count;
    //    //    var content = Content.Create(TestRoot);
    //    //    var actual = content.Children.DisableAutofilters().Count();
    //    //    Assert.AreEqual(expected, actual);
    //    //}


    [TestMethod]
    public async Task Linq_InFolder()
    {
        await LinqTest(repository =>
        {
            var folder1 = repository.CreateContent("/Root", nameof(Folder), "Folder1");
            folder1.Path = "/Root/Folder1";

            var expected = "InFolder:/root/folder1/cars";
            Assert.AreEqual(expected, GetQueryString(repository.Content.Where(c => c.InFolder(folder1.Path + "/Cars"))));
            Assert.AreEqual(expected, GetQueryString(from x in repository.Content where x.InFolder(folder1.Path + "/Cars") select x));
        });
    }
    [TestMethod]
    public async Task Linq_InTree()
    {
        await LinqTest(repository =>
        {
            var ims = repository.CreateContent("/Root", nameof(SystemFolder), "IMS");
            ims.Path = "/Root/IMS";

            Assert.AreEqual($"InTree:{ims.Path.ToLowerInvariant()}", GetQueryString(repository.Content.Where(c => c.InTree(ims))));
        });
    }
    [TestMethod]
    public async Task Linq_TypeFilter_Strong()
    {
        await LinqTest(repository =>
        {
            var name = "folder1";
            var root = repository.CreateContent("/Root", nameof(Folder), name);
            root.Path = $"/Root/{name}";

            //-- type that handles one content type
            var expected = $"+TypeIs:group +InTree:/root/{name} .SORT:Name .AUTOFILTERS:OFF";
            Assert.AreEqual(expected, GetQueryString(repository.Content.DisableAutofilters().Where(c => c.InTree(root) && c is Group).OrderBy(c => c.Name)));
            Assert.AreEqual(expected, GetQueryString(from c in repository.Content.DisableAutofilters() where c.InTree(root) && c is Group orderby c.Name select c));

            Assert.AreEqual(expected, GetQueryString(repository.Content.DisableAutofilters().Where(c => c.InTree(root) && typeof(Group).IsAssignableFrom(c.GetType())).OrderBy(c => c.Name)));
            Assert.AreEqual(expected, GetQueryString(from c in repository.Content.DisableAutofilters() where c.InTree(root) && typeof(Group).IsAssignableFrom(c.GetType()) orderby c.Name select c));

            //-- type that handles more than one content type
            expected = $"+TypeIs:genericcontent +InTree:/root/{name}/cars .SORT:Name .AUTOFILTERS:OFF";
            Assert.AreEqual(expected, GetQueryString(repository.Content.DisableAutofilters().Where(c => c.InTree(root.Path + "/Cars") && c is Content).OrderBy(c => c.Name)));
            Assert.AreEqual(expected, GetQueryString(from c in repository.Content.DisableAutofilters() where c.InTree(root.Path + "/Cars") && c is Content orderby c.Name select c));

            Assert.AreEqual(expected, GetQueryString(repository.Content.DisableAutofilters().Where(c => c.InTree(root.Path + "/Cars") && typeof(Content).IsAssignableFrom(c.GetType())).OrderBy(c => c.Name)));
            Assert.AreEqual(expected, GetQueryString(from c in repository.Content.DisableAutofilters() where c.InTree(root.Path + "/Cars") && typeof(Content).IsAssignableFrom(c.GetType()) orderby c.Name select c));
        });
    }

    [TestMethod]
    public async Task Linq_TypeFilter_String()
    {
        await LinqTest(repository =>
        {
            Assert.AreEqual("Type:group",
                GetQueryString(repository.Content.Where(c => c.Type == "Group")));
            Assert.AreEqual("+Id:>0 +Type:group",
                GetQueryString(repository.Content.Where(c => c.Type == "Group" && c.Id > 0)));
            Assert.AreEqual("Type:group",
                GetQueryString(repository.Content.Where(c => c.Type == typeof(Group).Name)));
            Assert.AreEqual("TypeIs:group",
                GetQueryString(repository.Content.Where(c => c is Group)));
        });
    }

    [TestMethod]
    public async Task Linq_ConditionalOperator()
    {
        await LinqTest(repository =>
        {
            // First operand of the conditional operator is a constant
            var bool1 = true;
            Assert.AreEqual("DisplayName:car", GetQueryString(repository.Content.Where(c => bool1 ? c.DisplayName == "Car" : c.Index == 42)));

            var bool2 = false;
            Assert.AreEqual("Index:42", GetQueryString(repository.Content.Where(c => bool2 ? c.DisplayName == "Car" : c.Index == 42)));

            // First operand is not a constant
            Assert.AreEqual("(+Index:85 -Type:car) (+DisplayName:ferrari +Type:car)",
                GetQueryString(repository.Content.Where(c => c.Type == "Car" ? c.DisplayName == "Ferrari" : c.Index == 85)));

            Assert.AreEqual("(+Index:85 -Type:car) (+DisplayName:'my nice ferrari' +Type:car)",
                GetQueryString(repository.Content.Where(c => c.Type == "Car" ? c.DisplayName == "My nice Ferrari" : c.Index == 85)));
        });
    }

    [TestMethod]
    public async Task Linq_FieldWithIndexer()
    {
        await LinqTest(repository =>
        {
            var root = repository.CreateContent("/Root", "Folder", "Folder1");
            root.Path = "/Root/Folder1";

            var expected = "+DisplayName:porsche +InTree:/root/folder1 .SORT:Name .AUTOFILTERS:OFF";
            Assert.AreEqual(expected, GetQueryString(
                repository.Content.DisableAutofilters()
                    .Where(c => c.InTree(root) && (string)c["DisplayName"] == "Porsche")
                    .OrderBy(c => c.Name)));
            Assert.AreEqual(expected, GetQueryString(from c in repository.Content.DisableAutofilters()
                                                     where c.InTree(root) && (string)c["DisplayName"] == "Porsche"
                                                     orderby c.Name
                                                     select c));
        });
    }

    [TestMethod]
    public async Task Linq_Boolean()
    {
        await LinqTest(repository =>
        {
            var root = repository.CreateContent("/Root", "Folder", "Folder1");
            root.Path = "/Root/Folder1";

            var expected = "+((+DisplayName:ferrari +Index:4) (+DisplayName:porsche +Index:2)) +InTree:/root/folder1 .SORT:Name .AUTOFILTERS:OFF";
            Assert.AreEqual(expected,
                GetQueryString(repository.Content.DisableAutofilters().Where(c => c.InTree(root) && (((int)c["Index"] == 2 && (string)c["DisplayName"] == "Porsche") || ((int)c["Index"] == 4 && (string)c["DisplayName"] == "Ferrari"))).OrderBy(c => c.Name)));
            Assert.AreEqual(expected,
                GetQueryString(from c in repository.Content.DisableAutofilters() where c.InTree(root) && (((int)c["Index"] == 2 && (string)c["DisplayName"] == "Porsche") || ((int)c["Index"] == 4 && (string)c["DisplayName"] == "Ferrari")) orderby c.Name select c));
        });
    }

    [TestMethod]
    public async Task Linq_AndOrPrecedence()
    {
        await LinqTest(repository =>
        {
            var root = repository.CreateContent("/Root", "Folder", "Folder1");
            root.Path = "/Root/Folder1";

            Assert.AreEqual("+(Index:3 (+Index:2 +TypeIs:group)) +InTree:/root/folder1",
                GetQueryString(
                    repository.Content.Where(
                        c => c.InTree(root) && (c is Group && c.Index == 2 || c.Index == 3))));
            Assert.AreEqual("+((+TypeIs:group +Index:3) Index:2) +InTree:/root/folder1",
                GetQueryString(
                    repository.Content.Where(
                        c => c.InTree(root) && (c.Index == 2 || c.Index == 3 && c is Group))));
        });
    }

    [TestMethod]
    public async Task Linq_OrderBy()
    {
        await LinqTest(repository =>
        {
            var root = repository.CreateContent("/Root", "Folder", "Folder1");
            root.Path = "/Root/Folder1";

            Assert.AreEqual("+TypeIs:folder +InTree:/root/folder1 .SORT:Index .REVERSESORT:Name .AUTOFILTERS:OFF",
                GetQueryString(repository.Content.DisableAutofilters()
                    .Where(c => c.InTree(root) && c is Folder)
                    .OrderBy(c => c.Index)
                    .ThenByDescending(c => c.Name)));
        });
    }

    /* ------------------------------------------------------------------------------------------- */

    [TestMethod]
    public async Task Linq_Projection_FluentApi()
    {
        await LinqTest(repository =>
        {
            var request = GetODataRequest(repository.Content
                .EnableAutofilters()
                .CountOnly()
                .DisableLifespan()
                .ExpandFields(nameof(User.Manager), "Manager/CreatedBy")
                .SelectFields("Id", "Domain", "LoginName", "Email", "Manager/Name", "Manager/CreatedBy/Name")
                .Where(c => c.Id < 100 && c is User));
            Assert.IsNotNull(request.Expand);
            Assert.AreEqual(2, request.Expand.Count());
            Assert.AreEqual("Manager,Manager/CreatedBy", string.Join(",", request.Expand));
            Assert.IsNotNull(request.Select);
            Assert.AreEqual(6, request.Select.Count());
            Assert.AreEqual("Id,Domain,LoginName,Email,Manager/Name,Manager/CreatedBy/Name", string.Join(",", request.Select));
            Assert.AreEqual(FilterStatus.Enabled, request.AutoFilters);
            Assert.AreEqual(FilterStatus.Disabled, request.LifespanFilter);
            Assert.AreEqual(InlineCountOptions.AllPages, request.InlineCount);
        });
    }

    [TestMethod]
    [ExpectedException(typeof(NotSupportedException))]
    public async Task Linq_Projection_NoSelect_NoExpand()
    {
        await LinqTest(repository =>
        {
            var expression = repository.Content
                .EnableAutofilters()
                .CountOnly()
                .DisableLifespan()
                .Where(c => c.Id < 100 && c is User)
                .OfType<User>()
                .Select(u => Content.Create<User>());

            var request = GetODataRequest(expression);
            Assert.Fail("The expected exception was not thrown.");
        });
    }
    [TestMethod]
    public async Task Linq_Projection_Select_NoExpand()
    {
        await LinqTest(repository =>
        {
            var expression = repository.Content
                .EnableAutofilters()
                .CountOnly()
                .DisableLifespan()
                .Where(c => c.Id < 100 && c is User)
                .OfType<User>()
                .Select(u => Content.Create<User>(u.Id, u.Domain, u.LoginName, u.Email));

            var request = GetODataRequest(expression);

            Assert.IsNull(request.Expand);
            Assert.AreEqual("Id,Domain,LoginName,Email",
                string.Join(",", request.Select ?? Array.Empty<string>()));
        });
    }
    [TestMethod]
    public async Task Linq_Projection_Select_Expand()
    {
        await LinqTest(repository =>
        {
            var expression = repository.Content
                .EnableAutofilters()
                .CountOnly()
                .DisableLifespan()
                .Where(c => c.Id < 100 && c is User)
                .OfType<User>()
                .Select(u => Content.Create<User>(
                    u.Id,
                    u.Domain,
                    u.LoginName,
                    u.Email,
                    u.Manager.Id,
                    u.Manager.Type,
                    u.Manager.Path,
                    u.Manager.FullName,
                    u.CreatedBy.Id,
                    u.CreatedBy.Type,
                    u.CreatedBy.Path,
                    u.CreatedBy.FullName,
                    u.ModifiedBy));

            var request = GetODataRequest(expression);

            Assert.AreEqual("Manager,CreatedBy,ModifiedBy",
                string.Join(",", request.Expand ?? Array.Empty<string>()));
            Assert.AreEqual("Id,Domain,LoginName,Email,Manager/Id,Manager/Type,Manager/Path,Manager/FullName,CreatedBy/Id,CreatedBy/Type,CreatedBy/Path,CreatedBy/FullName,ModifiedBy",
                string.Join(",", request.Select ?? Array.Empty<string>()));
        });
    }

    [TestMethod]
    public async Task Linq_Projection_Select_NotProperty_1_NotSupported()
    {
        await LinqTest(repository =>
        {
            var expression = repository.Content
                .OfType<User>()
                .Select(u => Content.Create<User>(u.Domain, u.Id + 10, u.Email));

            try
            {
                // ACT
                var request = GetODataRequest(expression);
                Assert.Fail("The expected exception was not thrown.");
            }
            catch (NotSupportedException e)
            {
                // ASSERT
                Assert.AreEqual("Invalid Select expression. The second parameter is forbidden." +
                                " Only the property-access expressions are allowed.", e.Message);
            }
        });
    }
    [TestMethod]
    public async Task Linq_Projection_Select_NotProperty_2_NotSupported()
    {
        await LinqTest(repository =>
        {
            var user = repository.CreateContent<User>("/Root/IMS/Public", "User", "U1");
            var expression = repository.Content
                .OfType<User>()
                .Select(u => Content.Create<User>(user.Name, u.Email));

            try
            {
                var request = GetODataRequest(expression);
                Assert.Fail("The expected exception was not thrown.");
            }
            catch (NotSupportedException e)
            {
                // ASSERT
                Assert.AreEqual("Invalid Select expression. The first parameter is forbidden. " +
                                "Only the property-access expressions are allowed.", e.Message);
            }
        });
    }

    [TestMethod]
    public async Task Linq_Projection_Select_NotCreation_NotSupported()
    {
        await LinqTest(repository =>
        {
            try
            {
                // ACT
                var expression = repository.Content
                    .OfType<User>()
                    .Select(u => u.Domain);

                Assert.Fail("The expected exception was not thrown.");
            }
            catch (NotSupportedException e)
            {
                // ASSERT
                Assert.AreEqual("Cannot resolve an expression. Use 'AsEnumerable()' " +
                                "method before calling the 'Select' method. ", e.Message);
            }
        });
    }
    [TestMethod]
    public async Task Linq_Projection_Select_ExecutedCreation_TargetInvocationException()
    {
        await LinqTest(repository =>
        {
            var expression = repository.Content
                .OfType<User>()
                .Select(u => Content.Create<User>("u.Domain"));

            try
            {
                var request = GetODataRequest(expression);
                Assert.Fail("The expected exception was not thrown.");
            }
            catch (TargetInvocationException e)
            {
                // ASSERT
                Assert.AreEqual("This method is used in processing LINQ expressions only. Do not use it in your code.", e.InnerException?.Message);
            }
        });
    }

    [TestMethod]
    public async Task Linq_Projection_Select_StringLiteral_NotSupported()
    {
        await LinqTest(repository =>
        {
            var expression = repository.Content
                .OfType<User>()
                .Select(u => Content.Create<User>(u.Name, u.Email, "u.Domain"));

            try
            {
                var request = GetODataRequest(expression);
                Assert.Fail("The expected exception was not thrown.");
            }
            catch (NotSupportedException e)
            {
                // ASSERT
                Assert.AreEqual("Invalid Select expression. The third parameter is forbidden. " +
                                "Only the property-access expressions are allowed.", e.Message);
            }
        });
    }
    [TestMethod]
    public async Task Linq_Projection_Select_TooManyLambdaParam_NotSupported()
    {
        await LinqTest(repository =>
        {
            var expression = repository.Content
                .OfType<User>()
                .Select((u, i) => Content.Create<User>(u.Name, u.Email, "u.Domain"));

            try
            {
                var request = GetODataRequest(expression);
                Assert.Fail("The expected exception was not thrown.");
            }
            catch (NotSupportedException e)
            {
                // ASSERT
                Assert.AreEqual("Invalid Select expression. Too many lambda parameters.", e.Message);
            }
        });
    }

    //    //[TestMethod]
    //    //public async Task Linq_SelectSimple()
    //    //{
    //    //    var names = String.Join(", ",
    //    //        Content.All
    //    //        .Where(c => c.Id < 10).OrderBy(c => c.Name)
    //    //        .AsEnumerable()
    //    //        .Select(c => c.Name)
    //    //        );
    //    //    Assert.AreEqual("Admin, Administrators, BuiltIn, Everyone, IMS, Owners, Portal, Root, Visitor", names);
    //    //}
    //    //[TestMethod]
    //    //public async Task Linq_Select_WithoutAsEnumerable()
    //    //{
    //    //    try
    //    //    {
    //    //        var x = String.Join(", ", repository.Content.Where(c => c.Id < 10).OrderBy(c => c.Name).Select(c => c.Name));
    //    //        Assert.Fail("An error must be thrown with exclamation: Use AsEnumerable ...");
    //    //    }
    //    //    catch (NotSupportedException e)
    //    //    {
    //    //        if (!e.Message.Contains("AsEnumerable"))
    //    //            Assert.Fail("Exception message does not contain 'AsEnumerable'");
    //    //    }
    //    //}
    //    //[TestMethod]
    //    //public async Task Linq_SelectNew()
    //    //{
    //    //    var x = repository.Content.Where(c => c.Id < 10).OrderBy(c => c.Id).AsEnumerable().Select(c => new { Id = c.Id, c.Name }).ToArray();
    //    //    var y = String.Join(", ", x.Select(a => String.Concat(a.Id, ", ", a.Name)));
    //    //    Assert.AreEqual("1, Admin, 2, Root, 3, IMS, 4, BuiltIn, 5, Portal, 6, Visitor, 7, Administrators, 8, Everyone, 9, Owners", y);
    //    //}

    /* ------------------------------------------------------------------------------------------- */

    [TestMethod]
    public async Task Linq_OfType()
    {
        await LinqTest(repository =>
        {
            Assert.AreEqual("TypeIs:contenttype .AUTOFILTERS:OFF",
                GetQueryString(repository.Content.DisableAutofilters().OfType<ContentType>()));
        });
    }

    [TestMethod]
    public async Task Linq_TakeSkip()
    {
        await LinqTest(repository =>
        {
            Assert.AreEqual("IsFolder:yes .TOP:5 .SKIP:8", GetQueryString(repository.Content.Where(c => c.IsFolder == true).Skip(8).Take(5)));
        });
    }

    //UNDONE:LINQ: Template replacements are not supported on the client side
    //[TestMethod] 
    //public async Task Linq_ReplaceTemplates()
    //{
    //    await LinqTest(repository =>
    //    {
    //        var childrenDef = new ChildrenDefinition
    //        {
    //            PathUsage = PathUsageMode.InFolderOr,
    //            ContentQuery = "CreationDate:<@@CurrentDay@@"
    //        };
    //        var expr = repository.Content.Where(c => c.IsFolder == true).Expression;
    //        var actual = SnExpression.BuildQuery(expr, typeof(Content), "/Root/FakePath", childrenDef).ToString();
    //        var expected = $"(+IsFolder:yes +CreationDate:<'{DateTime.UtcNow.Date:yyyy-MM-dd} 00:00:00.0000') InFolder:/root/fakepath";

    //        Assert.AreEqual(expected, actual);
    //    });
    //}

    [TestMethod]
    public async Task Linq_API()
    {
        await LinqTest(repository =>
        {
            ContentSet<Content>[] contentSets =
            {
                        (ContentSet<Content>) repository.Content.Where(c => c.Id == 2),
                        (ContentSet<Content>) repository.Content.Where(c => c.Id == 2).Skip(5),
                        (ContentSet<Content>) repository.Content.Where(c => c.Id == 2).Take(5),
                        (ContentSet<Content>) repository.Content.Where(c => c.Id == 2).OrderBy(c => c.Name),
                        (ContentSet<Content>) repository.Content.Where(c => c.Id == 2).OrderByDescending(c => c.Name),
                        (ContentSet<Content>) repository.Content.Where(c => c.Id == 2).OrderBy(c => c.Name).ThenBy(c => c.Id),
                        (ContentSet<Content>) repository.Content.Where(c => c.Id == 2).OrderBy(c => c.Name).ThenByDescending(c => c.Id),
                        (ContentSet<Content>) repository.Content.EnableAutofilters().Where(c => c.Id == 2),
                        (ContentSet<Content>) repository.Content.DisableAutofilters().Where(c => c.Id == 2),
                        (ContentSet<Content>) repository.Content.EnableLifespan().Where(c => c.Id == 2),
                        (ContentSet<Content>) repository.Content.DisableLifespan().Where(c => c.Id == 2),
                        (ContentSet<Content>) repository.Content.EnableAutofilters().EnableLifespan().Where(c => c.Id == 2),
                        (ContentSet<Content>) repository.Content.EnableAutofilters().DisableLifespan().Where(c => c.Id == 2),
                        (ContentSet<Content>) repository.Content.DisableAutofilters().EnableLifespan().Where(c => c.Id == 2),
                        (ContentSet<Content>) repository.Content.DisableAutofilters().DisableLifespan().Where(c => c.Id == 2),
                        (ContentSet<Content>) repository.Content.EnableLifespan().EnableAutofilters().Where(c => c.Id == 2),
                        (ContentSet<Content>) repository.Content.DisableLifespan().EnableAutofilters().Where(c => c.Id == 2),
                        (ContentSet<Content>) repository.Content.EnableLifespan().DisableAutofilters().Where(c => c.Id == 2),
                        (ContentSet<Content>) repository.Content.DisableLifespan().DisableAutofilters().Where(c => c.Id == 2),
                        (ContentSet<Content>) repository.Content.SetExecutionMode(QueryExecutionMode.Default).Where(c => c.Id == 2),
                        (ContentSet<Content>) repository.Content.SetExecutionMode(QueryExecutionMode.Strict).Where(c => c.Id == 2),
                        (ContentSet<Content>) repository.Content.SetExecutionMode(QueryExecutionMode.Quick).Where(c => c.Id == 2),
                };

            var queries = new string[contentSets.Length];
            for (var i = 0; i < contentSets.Length; i++)
                queries[i] = GetQueryString(contentSets[i]);

            var expected = @"Id:2
Id:2 .SKIP:5
Id:2 .TOP:5
Id:2 .SORT:Name
Id:2 .REVERSESORT:Name
Id:2 .SORT:Name .SORT:Id
Id:2 .SORT:Name .REVERSESORT:Id
Id:2
Id:2 .AUTOFILTERS:OFF
Id:2 .LIFESPAN:ON
Id:2
Id:2 .LIFESPAN:ON
Id:2
Id:2 .AUTOFILTERS:OFF .LIFESPAN:ON
Id:2 .AUTOFILTERS:OFF
Id:2 .LIFESPAN:ON
Id:2
Id:2 .AUTOFILTERS:OFF .LIFESPAN:ON
Id:2 .AUTOFILTERS:OFF
Id:2
Id:2
Id:2 .QUICK";

            var actual = String.Join("\r\n", queries);
            Assert.AreEqual(expected, actual);
        });
    }

    [TestMethod]
    public async Task Linq_ExecutionMode_Quick()
    {
        await LinqTest(repository =>
        {
            ContentSet<Content>[] contentSets =
            {
                (ContentSet<Content>) repository.Content.SetExecutionMode(QueryExecutionMode.Default).Where(c => c.Id < 42),
                (ContentSet<Content>) repository.Content.SetExecutionMode(QueryExecutionMode.Strict).Where(c => c.Id < 42),
                (ContentSet<Content>) repository.Content.SetExecutionMode(QueryExecutionMode.Quick).Where(c => c.Id < 42),
            };
            var queries = new string[contentSets.Length];
            for (var i = 0; i < contentSets.Length; i++)
            {
                queries[i] = GetQueryString(contentSets[i]);
            }

            var expected = @"Id:<42
Id:<42
Id:<42 .QUICK";
            var actual = String.Join("\r\n", queries);
            Assert.AreEqual(expected, actual);
        });
    }

    //UNDONE:LINQ: activate the test
    //[TestMethod]
    public async Task Linq_Error_NotConstants()
    {
        await LinqTest(repository =>
        {
            try { var _ = repository.Content.Where(c => c.DisplayName.StartsWith(c.Name)).ToArray(); Assert.Fail("#1 Exception wasn't thrown"); } catch (NotSupportedException) { }
            try { var _ = repository.Content.Where(c => c.DisplayName.EndsWith(c.Name)).ToArray(); Assert.Fail("#2 Exception wasn't thrown"); } catch (NotSupportedException) { }
            try { var _ = repository.Content.Where(c => c.DisplayName.Contains(c.Name)).ToArray(); Assert.Fail("#3 Exception wasn't thrown"); } catch (NotSupportedException) { }

            try { var _ = repository.Content.Where(c => c.Type == c.DisplayName).ToArray(); Assert.Fail("#4 Exception wasn't thrown"); } catch (NotSupportedException) { }
            try { var _ = repository.Content.Where(c => c.InFolder(c.Path)).ToArray(); Assert.Fail("#5 Exception wasn't thrown"); } catch (NotSupportedException) { }
            try { var _ = repository.Content.Where(c => c.InTree(c.Path)).ToArray(); Assert.Fail("#6 Exception wasn't thrown"); } catch (NotSupportedException) { }
        });
    }

    //UNDONE:LINQ: This is an integration test
    //[TestMethod]
    //public async Task Linq_All()
    //{
    //    await LinqTest(repository =>
    //    {
    //        var expectedNonSystemCount = CreateSafeContentQuery(
    //            "InTree:/Root .COUNTONLY .AUTOFILTERS:ON", QuerySettings.Default).Execute().Count;

    //        var expectedAllCount = CreateSafeContentQuery(
    //            "+InTree:/Root .COUNTONLY .AUTOFILTERS:OFF", QuerySettings.Default).Execute().Count;

    //        // ---------------- TEST CASE 1: All non-system contents in ad-hoc order
    //        var contentArray = repository.Content.ToArray();
    //        // ASSERT
    //        Assert.AreEqual(expectedNonSystemCount, contentArray.Length);
    //        contentArray = contentArray.OrderBy(c => c.Id).ToArray();
    //        for (int i = 1; i < contentArray.Length; i++)
    //            Assert.IsTrue(contentArray[i - 1].Id < contentArray[i].Id);

    //        // ---------------- TEST CASE 2: All contents in ad-hoc order
    //        contentArray = repository.Content.DisableAutofilters().ToArray();
    //        // ASSERT
    //        Assert.AreEqual(expectedAllCount, contentArray.Length);
    //        contentArray = contentArray.OrderBy(c => c.Id).ToArray();
    //        for (int i = 1; i < contentArray.Length; i++)
    //            Assert.IsTrue(contentArray[i - 1].Id < contentArray[i].Id);

    //        // ---------------- TEST CASE 3: All non-system contents + sort
    //        contentArray = repository.Content.OrderByDescending(c => c.Id).ToArray();
    //        // ASSERT
    //        Assert.AreEqual(expectedNonSystemCount, contentArray.Length);
    //        for (int i = 1; i < contentArray.Length; i++)
    //            Assert.IsTrue(contentArray[i - 1].Id > contentArray[i].Id);

    //        // ---------------- TEST CASE 4: All contents + sort
    //        contentArray = repository.Content.DisableAutofilters().OrderByDescending(c => c.Id).ToArray();
    //        // ASSERT
    //        Assert.AreEqual(expectedAllCount, contentArray.Length);
    //        for (int i = 1; i < contentArray.Length; i++)
    //            Assert.IsTrue(contentArray[i - 1].Id > contentArray[i].Id);
    //    });
    //}

    /* ------------------------------------------------------------------------------------------- */

    [TestMethod]
    public async Task Linq_OptimizeBooleans()
    {
        await LinqTest(repository =>
        {
            var actual = GetQueryString(repository.Content.Where(c =>
                c.InFolder("/Root/FakePath") && (c.Name != "A" && ((c.Name != "B" && c.Name != "C") && c is Folder))));
            var expected = "+TypeIs:folder -Name:c -Name:b -Name:a +InFolder:/root/fakepath";
            Assert.AreEqual(expected, actual);
        });
    }

    /* ------------------------------------------------------------------------------------------- */

    [TestMethod]
    public async Task Linq_Tracer()
    {
        await LinqTest(repository =>
        {
            var tracer = new LinqTracer();
            var expected = "Expression: value(SenseNet.Client.Linq.ContentSet`1[SenseNet.Client.Content]).Where(c => c.InTree(\"/Root/IMS\")).Take(10)" +
                           "\r\nProperties: AutoFilters: Enabled, Lifespan: Default, Mode: Default, Expand: [null], Select: [null]" +
                           "\r\nCompiled: InTree:/root/ims .TOP:10\r\n";
            var actual = GetQueryString(repository.Content
                .EnableAutofilters()
                .SetTracer(tracer)
                .Where(c => c.InTree("/Root/IMS"))
                .Take(10));

            Assert.AreEqual(expected, tracer.Trace);
        });
    }

    //UNDONE:LINQ: This is an aspect-field test
    //[TestMethod]
    //public async Task Linq_AspectField()
    //{
    //    var aspectName = "Linq_AspectField_Aspect1";
    //    var fieldName = "Field1";
    //    var fieldValue = "fieldvalue";
    //    var aspectDefinition =
    //        $@"<AspectDefinition xmlns='http://schemas.sensenet.com/SenseNet/ContentRepository/AspectDefinition'>
    //              <Fields>
    //                <AspectField name='{fieldName}' type='ShortText' />
    //              </Fields>
    //            </AspectDefinition>";

    //    await LinqTest(repository =>
    //    {
    //        var aspect = new Aspect(Repository.AspectsFolder)
    //        {
    //            Name = aspectName,
    //            AspectDefinition = aspectDefinition
    //        };
    //        aspect.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();

    //        Assert.AreEqual($"{aspectName}.{fieldName}:{fieldValue}",
    //            GetQueryString(repository.Content.OfType<Content>()
    //                .Where(c => (string)c[$"{aspectName}.{fieldName}"] == fieldValue)));
    //    });
    //}

    /* ========================================================================================== Linq_NotSupported_ */

    [TestMethod]
    public async Task Linq_NotSupported_Select_New()
    {
        await AsEnumerableError("Select", repository =>
        {
            User[] users;
            var queryable = repository.Content.Where(c => c.Id < 10)
                .Select(c => new { c.Id, c.Path });
        });
    }
    [TestMethod]
    public async Task Linq_NotSupported_Select_Content()
    {
        await AsEnumerableError("Select", repository =>
        {
            var unused = repository.Content.Where(c => false)
                .Select(c => c.Id).ToArray();
        });
    }
    [TestMethod]
    public async Task Linq_NotSupported_SelectMany()
    {
        await AsEnumerableError("SelectMany", repository =>
        {
            var unused = repository.Content.Where(c => false)
                .SelectMany(c => c.Versions).ToArray();
        });
    }
    [TestMethod]
    public async Task Linq_NotSupported_SelectManySelect()
    {
        await AsEnumerableError("SelectMany", repository =>
        {
            var unused = repository.Content.Where(c => true)
                .SelectMany(c => c.Versions)
                .Select(c => c.CreatedBy).ToArray();
        });
    }
    [TestMethod]
    public async Task Linq_NotSupported_Min()
    {
        await AsEnumerableError("Min", repository =>
        {
            var unused = repository.Content.Where(c => true).Min(c => c.Id);
        });
    }
    [TestMethod]
    public async Task Linq_NotSupported_Max()
    {
        await AsEnumerableError("Max", repository =>
        {
            var unused = repository.Content.Where(c => true).Max(c => c.Id);
        });
    }
    [TestMethod]
    public async Task Linq_NotSupported_Sum()
    {
        await AsEnumerableError("Sum", repository =>
        {
            var unused = repository.Content.Where(c => true).Sum(c => c.Id);
        });
    }
    [TestMethod]
    public async Task Linq_NotSupported_Average()
    {
        await AsEnumerableError("Average", repository =>
        {
            var unused = repository.Content.Where(c => true).Average(c => c.Id);
        });
    }
    [TestMethod]
    public async Task Linq_NotSupported_SkipWhile()
    {
        await AsEnumerableError("SkipWhile", repository =>
        {
            var unused = repository.Content.Where(c => true).SkipWhile(c => false).ToArray();
        });
    }
    [TestMethod]
    public async Task Linq_NotSupported_TakeWhile()
    {
        await AsEnumerableError("TakeWhile", repository =>
        {
            var unused = repository.Content.Where(c => true).TakeWhile(c => false).ToArray();
        });
    }
    [TestMethod]
    public async Task Linq_NotSupported_Join_NonameOutput()
    {
        await AsEnumerableError("Join", repository =>
        {
            var unused = repository.Content.Where(c => c.Id < 10000)
                .Join(
                    repository.Content,
                    user => user.Id,
                    doc => doc.ModifiedBy.Id,
                    (user, doc) => new { UserId = user.Id, DocId = doc.Id })
                .Where(item => item.UserId == 42);
        });
    }
    [TestMethod]
    public async Task Linq_NotSupported_Join_ContentOutput()
    {
        await AsEnumerableError("Join", repository =>
        {
            var unused = repository.Content.Where(c => false)
                .Join(
                    repository.Content,
                    user => user.Id,
                    doc => doc.ModifiedBy.Id,
                    (user, doc) => user)
                .Where(user => user.Id == 42);
        });
    }
    [TestMethod]
    public async Task Linq_NotSupported_Aggregate()
    {
        await AsEnumerableError("Aggregate", repository =>
        {
            var unused = repository.Content.Where(c => false).Aggregate(0, (a, b) => a + b.Id);
        });
    }
    [TestMethod]
    public async Task Linq_NotSupported_Cast()
    {
        await AsEnumerableError("Cast", repository =>
        {
            var unused = repository.Content.Where(c => false).Cast<Content>().ToArray();
        });
    }
    [TestMethod]
    public async Task Linq_NotSupported_Distinct()
    {
        await AsEnumerableError("Distinct", repository =>
        {
            var unused = repository.Content.Where(c => false).Distinct().ToArray();
        });
    }
    [TestMethod]
    public async Task Linq_NotSupported_Concat()
    {
        await AsEnumerableError("Concat", repository =>
        {
            var unused = repository.Content.Where(c => false).Concat(new Content[0]).ToArray();
        });
    }
    [TestMethod]
    public async Task Linq_NotSupported_Union()
    {
        await AsEnumerableError("Union", repository =>
        {
            var unused = repository.Content.Where(c => false).Union(new Content[0]).ToArray();
        });
    }
    [TestMethod]
    public async Task Linq_NotSupported_Intersect()
    {
        await AsEnumerableError("Intersect", repository =>
        {
            var unused = repository.Content.Where(c => false).Intersect(new Content[0]).ToArray();
        });
    }
    [TestMethod]
    public async Task Linq_NotSupported_Except()
    {
        await AsEnumerableError("Except", repository =>
        {
            var unused = repository.Content.Where(c => false).Except(new Content[0]).ToArray();
        });
    }
    [TestMethod]
    public async Task Linq_NotSupported_GroupBy()
    {
        await AsEnumerableError("GroupBy", repository =>
        {
            var unused = repository.Content.Where(c => false).GroupBy(c => c);
        });
    }
    [TestMethod]
    public async Task Linq_NotSupported_GroupJoin()
    {
        await AsEnumerableError("GroupJoin", repository =>
        {
            var unused = repository.Content.Where(c => false)
                .GroupJoin(new Content[0], a => a, b => b, (c, d) => new { A = c, B = d });
        });
    }
    [TestMethod]
    public async Task Linq_NotSupported_All()
    {
        await AsEnumerableError("All", repository =>
        {
            var unused = repository.Content.Where(c => false).All(c => true);
        });
    }
    [TestMethod]
    public async Task Linq_NotSupported_Reverse()
    {
        await AsEnumerableError("Reverse", repository =>
        {
            var unused = repository.Content.Where(c => false).Reverse().ToArray();
        });
    }
    [TestMethod]
    public async Task Linq_NotSupported_SequenceEqual()
    {
        await AsEnumerableError("SequenceEqual", repository =>
        {
            var unused = repository.Content.Where(c => false).SequenceEqual(new Content[0]);
        });
    }
    [TestMethod]
    public async Task Linq_NotSupported_DefaultIfEmpty()
    {
        await AsEnumerableError("DefaultIfEmpty", repository =>
        {
            var unused = repository.Content.Where(c => false).DefaultIfEmpty().ToArray();
        });
    }
    [TestMethod]
    public async Task Linq_NotSupported_DefaultIfEmptyDefault()
    {
        await AsEnumerableError("DefaultIfEmpty", repository =>
        {
            Content defaultContent = null;
            var unused = repository.Content.Where(c => false).DefaultIfEmpty(defaultContent).ToArray();
        });
    }

    [TestMethod]
    public async Task Linq_NotSupported_Zip()
    {
        await AsEnumerableError("Zip", repository =>
        {
            var unused = repository.Content.Where(c => false).Zip(new Content[0], (a, b) => a).ToArray();
        });
    }


    /* ========================================================================================== Linq_Exec_ */

    [TestMethod]
    public async Task Linq_Exec_ToArray()
    {
        var mockResult = @"{
  ""d"": {
    ""__count"": 9,
    ""results"": [
      { ""Id"": 1, ""Name"": ""C1"", ""Type"": ""Content"" },
      { ""Id"": 2, ""Name"": ""C2"", ""Type"": ""Content"" },
      { ""Id"": 3, ""Name"": ""C3"", ""Type"": ""Content"" },
      { ""Id"": 4, ""Name"": ""C4"", ""Type"": ""Content"" },
      { ""Id"": 5, ""Name"": ""C5"", ""Type"": ""Content"" },
      { ""Id"": 6, ""Name"": ""C6"", ""Type"": ""Content"" },
      { ""Id"": 7, ""Name"": ""C7"", ""Type"": ""Content"" },
      { ""Id"": 8, ""Name"": ""C8"", ""Type"": ""Content"" },
      { ""Id"": 9, ""Name"": ""C9"", ""Type"": ""Content"" }
    ]
  }
}";
        await LinqExecutionTest(mockResult, repository =>
        {
            var result = repository.Content
                .DisableAutofilters()
                .Where(c => c.Id < 10)
                .OrderBy(c => c.Id).ToArray();
            Assert.AreEqual(result.Select(c=>c.GetType()).Distinct().Single().FullName, typeof(Content).FullName);
            Assert.AreEqual("1,2,3,4,5,6,7,8,9", string.Join(",", result.Select(c => c.Id)));
        });
    }

    string _emptyResult = @"{""d"": {""__count"": 0,""results"": []}}";
    string _fiveResult = @"{
  ""d"": {
    ""__count"": 9,
    ""results"": [
      { ""Id"": 1, ""Name"": ""C1"", ""Type"": ""Content"" },
      { ""Id"": 2, ""Name"": ""C2"", ""Type"": ""Content"" },
      { ""Id"": 3, ""Name"": ""C3"", ""Type"": ""Content"" },
      { ""Id"": 4, ""Name"": ""C4"", ""Type"": ""Content"" },
      { ""Id"": 5, ""Name"": ""C5"", ""Type"": ""Content"" }
    ]
  }
}";
    string _fiveResultDescending = @"{
  ""d"": {
    ""__count"": 9,
    ""results"": [
      { ""Id"": 5, ""Name"": ""C5"", ""Type"": ""Content"" },
      { ""Id"": 4, ""Name"": ""C4"", ""Type"": ""Content"" },
      { ""Id"": 3, ""Name"": ""C3"", ""Type"": ""Content"" },
      { ""Id"": 2, ""Name"": ""C2"", ""Type"": ""Content"" },
      { ""Id"": 1, ""Name"": ""C1"", ""Type"": ""Content"" },
    ]
  }
}";
    string _nineResult = @"{
  ""d"": {
    ""__count"": 9,
    ""results"": [
      { ""Id"": 1, ""Name"": ""C1"", ""Type"": ""Content"" },
      { ""Id"": 2, ""Name"": ""C2"", ""Type"": ""Content"" },
      { ""Id"": 3, ""Name"": ""C3"", ""Type"": ""Content"" },
      { ""Id"": 4, ""Name"": ""C4"", ""Type"": ""Content"" },
      { ""Id"": 5, ""Name"": ""C5"", ""Type"": ""Content"" },
      { ""Id"": 6, ""Name"": ""C6"", ""Type"": ""Content"" },
      { ""Id"": 7, ""Name"": ""C7"", ""Type"": ""Content"" },
      { ""Id"": 8, ""Name"": ""C8"", ""Type"": ""Content"" },
      { ""Id"": 9, ""Name"": ""C9"", ""Type"": ""Content"" }
    ]
  }
}";
    string _nineResultDescending = @"{
  ""d"": {
    ""__count"": 9,
    ""results"": [
      { ""Id"": 9, ""Name"": ""C9"", ""Type"": ""Content"" },
      { ""Id"": 8, ""Name"": ""C8"", ""Type"": ""Content"" },
      { ""Id"": 7, ""Name"": ""C7"", ""Type"": ""Content"" },
      { ""Id"": 6, ""Name"": ""C6"", ""Type"": ""Content"" },
      { ""Id"": 5, ""Name"": ""C5"", ""Type"": ""Content"" },
      { ""Id"": 4, ""Name"": ""C4"", ""Type"": ""Content"" },
      { ""Id"": 3, ""Name"": ""C3"", ""Type"": ""Content"" },
      { ""Id"": 2, ""Name"": ""C2"", ""Type"": ""Content"" },
      { ""Id"": 1, ""Name"": ""C1"", ""Type"": ""Content"" },
    ]
  }
}";

    [TestMethod]
    public async Task Linq_Exec_First()
    {
        // ======== First()
        await LinqExecutionTest(_fiveResultDescending, repository =>
        {
            Assert.AreEqual(5, repository.Content.DisableAutofilters().Where(c => c.Id < 6).OrderByDescending(c => c.Id).First().Id);
            Assert.AreEqual(5, repository.Content.DisableAutofilters().Where(c => c.Id < 6).OrderByDescending(c => c.Id).First().Id);
            Assert.AreEqual(5, repository.Content.DisableAutofilters().Where(c => c.Id < 10).OrderByDescending(c => c.Id).First(c => c.Id < 6).Id);
        });

        // empty
        await LinqExecutionTest(_emptyResult, repository =>
        {
            InvalidOperationTest(() => { repository.Content.Where(c => c.Id < 0).First(); });
            InvalidOperationTest(() => { repository.Content.First(c => c.Id < 0); });
        });

        // ======== FirstOrDefault()
        await LinqExecutionTest(_fiveResultDescending, repository =>
        {
            Assert.AreEqual(5, repository.Content.DisableAutofilters().Where(c => c.Id < 6).OrderByDescending(c => c.Id).FirstOrDefault().Id);
            Assert.AreEqual(5, repository.Content.DisableAutofilters().Where(c => c.Id < 10).OrderByDescending(c => c.Id).FirstOrDefault(c => c.Id < 6).Id);
        });

        // empty
        await LinqExecutionTest(_emptyResult, repository =>
        {
            Assert.IsNull(repository.Content.Where(c => c.Id < 0).FirstOrDefault());
            Assert.IsNull(repository.Content.Where(c => c.Id < 0).FirstOrDefault(c => c.Id < 4));
        });
    }
    [TestMethod]
    public async Task Linq_Exec_Last()
    {
        // ======== Last()
        await LinqExecutionTest(_fiveResult, repository =>
        {
            Assert.AreEqual(5, repository.Content.DisableAutofilters().Where(c => c.Id < 6).OrderBy(c => c.Id).Last().Id);
            Assert.AreEqual(5, repository.Content.DisableAutofilters().Where(c => c.Id < 10).OrderBy(c => c.Id).Last(c => c.Id < 6).Id);
        });

        // empty
        await LinqExecutionTest(_emptyResult, repository =>
        {
            InvalidOperationTest(() => { repository.Content.Where(c => c.Id < 0).Last(); });
            InvalidOperationTest(() => { repository.Content.Last(c => c.Id < 0); });
        });

        // ======== LastOrDefault()
        await LinqExecutionTest(_fiveResult, repository =>
        {
            Assert.AreEqual(5, repository.Content.DisableAutofilters().Where(c => c.Id < 6).OrderBy(c => c.Id).LastOrDefault().Id);
            Assert.AreEqual(5, repository.Content.DisableAutofilters().Where(c => c.Id < 10).OrderBy(c => c.Id).LastOrDefault(c => c.Id < 6).Id);
        });

        // empty
        await LinqExecutionTest(_emptyResult, repository =>
        {
            Assert.IsNull(repository.Content.Where(c => c.Id < 0).LastOrDefault());
            Assert.IsNull(repository.Content.Where(c => c.Id < 0).LastOrDefault(c => c.Id < 4));
        });
    }
    [TestMethod]
    public async Task Linq_Exec_Single()
    {
        string mockResult = @"{""d"": {""__count"": 1,""results"": [{ ""Id"": 5, ""Name"": ""C5"", ""Type"": ""Content"" }]}}";

        // ======== Single()
        await LinqExecutionTest(mockResult, repository =>
        {
            Assert.AreEqual(5, repository.Content.DisableAutofilters().Where(c => c.Id == 5).Single().Id);
            Assert.AreEqual(5, repository.Content.DisableAutofilters().Single(c => c.Id == 5).Id);
        });

        // empty
        await LinqExecutionTest(_emptyResult, repository =>
        {
            InvalidOperationTest(() => { repository.Content.Where(c => c.Id < 0).Single(); });
            InvalidOperationTest(() => { repository.Content.Single(c => c.Id < 0); });
        });

        // more than one
        await LinqExecutionTest(_fiveResult, repository =>
        {
            InvalidOperationTest(() => { repository.Content.Where(c => c.Id < 6).Single(); });
            InvalidOperationTest(() => { repository.Content.Single(c => c.Id < 6); });
        });

        // ======== SingleOrDefault()
        await LinqExecutionTest(mockResult, repository =>
        {
            Assert.AreEqual(5, repository.Content.DisableAutofilters().Where(c => c.Id == 5).SingleOrDefault().Id);
            Assert.AreEqual(5, repository.Content.DisableAutofilters().SingleOrDefault(c => c.Id == 5).Id);
        });

        // empty
        await LinqExecutionTest(_emptyResult, repository =>
        {
            Assert.IsNull(repository.Content.Where(c => c.Id < 0).SingleOrDefault()); 
            Assert.IsNull(repository.Content.SingleOrDefault(c => c.Id < 0));
        });

        // more than one
        await LinqExecutionTest(_fiveResult, repository =>
        {
            InvalidOperationTest(() => { repository.Content.Where(c => c.Id < 6).SingleOrDefault(); });
            InvalidOperationTest(() => { repository.Content.SingleOrDefault(c => c.Id < 6); });
        });
    }
    [TestMethod]
    public async Task Linq_Exec_ElementAt()
    {
        var mockResult = @"{""d"": {""__count"": 1,""results"": [{ ""Id"": 6, ""Name"": ""C6"", ""Type"": ""Content"" }]}}";

        // ======== ElementAt()
        await LinqExecutionTest(mockResult, repository =>
        {
            var x = 2;
            Assert.AreEqual(6, repository.Content.DisableAutofilters().Where(c => c.Id < 10).OrderBy(c => c.Id).ElementAt(3 + x).Id);
        });
        await LinqExecutionTest(_emptyResult, repository =>
        {
            // less
            ArgumentOutOfRangeTest(() => { repository.Content.DisableAutofilters().Where(c => c.Id < 3).OrderBy(c => c.Id).ElementAt(5); });
        });
        await LinqExecutionTest(_emptyResult, repository =>
        {
            // empty
            ArgumentOutOfRangeTest(() => { repository.Content.DisableAutofilters().Where(c => c.Id < 0).OrderBy(c => c.Id).ElementAt(5); });
        });

        // ======== ElementAtOrDefault()
        await LinqExecutionTest(mockResult, repository =>
        {
            Assert.AreEqual(6, repository.Content.DisableAutofilters().Where(c => c.Id < 10).OrderBy(c => c.Id).ElementAtOrDefault(5).Id);
        });
        await LinqExecutionTest(_emptyResult, repository =>
        {
            // less
            Assert.IsNull(repository.Content.DisableAutofilters().Where(c => c.Id < 3).OrderBy(c => c.Id).ElementAtOrDefault(5));
        });
        await LinqExecutionTest(_emptyResult, repository =>
        {
            // empty
            Assert.IsNull(repository.Content.DisableAutofilters().Where(c => c.Id < 0).OrderBy(c => c.Id).ElementAtOrDefault(5));
        });
    }


    [TestMethod]
    public async Task Linq_Exec_Any()
    {
        var oneResult = @"{""d"": {""__count"": 1,""results"": [{""Id"": 1,""Name"": ""Admin"",""Type"": ""Content""}]}}";
        // Where + Any
        await LinqExecutionTest(_emptyResult, repository =>
        {
            Assert.IsFalse(repository.Content.DisableAutofilters().Where(c => c.Id == 0).Any());
        });
        await LinqExecutionTest(oneResult, repository =>
        {
            Assert.IsTrue(repository.Content.DisableAutofilters().Where(c => c.Id == 1).Any());
        });
        await LinqExecutionTest(_fiveResult, repository =>
        {
            Assert.IsTrue(repository.Content.DisableAutofilters().Where(c => c.Id > 0).Any());
        });

        // only Any
        await LinqExecutionTest(_emptyResult, repository =>
        {
            Assert.IsFalse(repository.Content.DisableAutofilters().Any(c => c.Id == 0));
        });
        await LinqExecutionTest(oneResult, repository =>
        {
            Assert.IsTrue(repository.Content.DisableAutofilters().Any(c => c.Id == 1));
        });
        await LinqExecutionTest(_fiveResult, repository =>
        {
            Assert.IsTrue(repository.Content.DisableAutofilters().Any(c => c.Id > 0));
        });
    }
    [TestMethod]
    public async Task Linq_Exec_CountOnly()
    {
        await LinqExecutionTest(_nineResult, repository =>
        {
            Assert.AreEqual(9, repository.Content.DisableAutofilters().Where(c => c.Id < 10).Count());
            Assert.AreEqual(9, repository.Content.DisableAutofilters().Count(c => c.Id < 10));

            Assert.AreEqual(9L, repository.Content.DisableAutofilters().Where(c => c.Id < 10).LongCount());
            Assert.AreEqual(9L, repository.Content.DisableAutofilters().LongCount(c => c.Id < 10));
        });
    }
    [TestMethod]
    public async Task Linq_Exec_CountIsDeferred()
    {
        await LinqTest(repository =>
        {
            var tracer = new LinqTracer();
            var count = repository.Content
                .SetTracer(tracer)
                .DisableAutofilters()
                .Where(c => c.InFolder("/Root/Content"))
                .Count();
            Assert.IsTrue(tracer.Trace.Contains(".COUNTONLY"));

            tracer = new LinqTracer();
            count = repository.Content
                .SetTracer(tracer)
                .DisableAutofilters()
                .Count(c => c.InFolder("/Root/Content"));

        });
    }
    [TestMethod]
    public async Task Linq_Exec_OfTypeAndFirst()
    {
        var mockResult = @"{
  ""d"": {
    ""__count"": 2,
    ""results"": [
      { ""Id"": 1111, ""Name"": ""G1"", ""Type"": ""User"" },
      { ""Id"": 2222, ""Name"": ""G2"", ""Type"": ""User"" },
    ]
  }
}";

        await LinqExecutionTest(mockResult, repository =>
        {
            var result = repository.Content.OfType<User>()
                .FirstOrDefault(c => c.InTree("/Root/IMS") && c.Email == "user1@example.com");
            Assert.IsTrue(result != null);
        });
    }
    [TestMethod]
    public async Task Linq_Exec_OfTypeAndWhere()
    {
        var mockResult = @"{
  ""d"": {
    ""__count"": 9,
    ""results"": [
      { ""Id"": 1111, ""Name"": ""G1"", ""Type"": ""Group"" },
      { ""Id"": 2222, ""Name"": ""G2"", ""Type"": ""Group"" },
    ]
  }
}";

        await LinqExecutionTest(mockResult, repository =>
        {
            string path = "/Root/IMS/BuiltIn/Portal";
            var ok = repository.Content
                .OfType<Group>()
                .Where(g => g.InTree(path))
                .AsEnumerable()
                .Any(g => g.Id > 0); // something that can be executed locally
            Assert.IsTrue(ok);
        });

    }

    //    ///* ------------------------------------------------------------------------------------------- */ 


    //    //[TestMethod]
    //    //public void Linq_Exec_Error_UnknownField()
    //    //{
    //    //    try
    //    //    {
    //    //        var x = repository.Content.Where(c => (int)c["UnknownField"] == 42).ToArray();
    //    //        Assert.Fail("The expected InvalidOperationException was not thrown.");
    //    //    }
    //    //    catch (InvalidOperationException e)
    //    //    {
    //    //        var msg = e.Message;
    //    //        Assert.IsTrue(msg.ToLower().Contains("unknown field"), "Error message does not contain: 'unknown field'.");
    //    //        Assert.IsTrue(msg.Contains("UnknownField"), "Error message does not contain the field name: 'UnknownField'.");
    //    //    }
    //    //}


    //    /* ========================================================================================== bugz */

    //    [TestMethod]
    //    public async Task Linq_BugReproduction_OptimizeBooleans_1()
    //    {
    //        await LinqTest(repository =>
    //        {
    //            // +(TypeIs:group TypeIs:user) +InFolder:/root/ims/builtin/demo/managers

    //            var childrenDef = new ChildrenDefinition { PathUsage = PathUsageMode.InFolderAnd };
    //            var expr = repository.Content.Where(c => c.ContentHandler is Group || c.ContentHandler is User).Expression;
    //            var actual = SnExpression.BuildQuery(expr, typeof(Content), "/Root/FakePath", childrenDef).ToString();
    //            var expected = "+(TypeIs:user TypeIs:group) +InFolder:/root/fakepath";
    //            Assert.AreEqual(expected, actual);

    //            childrenDef = new ChildrenDefinition { PathUsage = PathUsageMode.InFolderAnd, ContentQuery = "Id:>0" };
    //            expr = repository.Content.Where(c => c.ContentHandler is Group || c.ContentHandler is User).Expression;
    //            actual = SnExpression.BuildQuery(expr, typeof(Content), "/Root/FakePath", childrenDef).ToString();
    //            expected = "+(TypeIs:user TypeIs:group) +Id:>0 +InFolder:/root/fakepath";
    //            Assert.AreEqual(expected, actual);

    //            childrenDef = new ChildrenDefinition { PathUsage = PathUsageMode.InFolderAnd, ContentQuery = "TypeIs:user TypeIs:group" };
    //            actual = SnExpression.BuildQuery(null, typeof(Content), "/Root/FakePath", childrenDef).ToString();
    //            expected = "+(TypeIs:user TypeIs:group) +InFolder:/root/fakepath";
    //            Assert.AreEqual(expected, actual);

    //            childrenDef = new ChildrenDefinition { PathUsage = PathUsageMode.InFolderAnd, ContentQuery = "+(TypeIs:user TypeIs:group)" };
    //            actual = SnExpression.BuildQuery(null, typeof(Content), "/Root/FakePath", childrenDef).ToString();
    //            expected = "+(TypeIs:user TypeIs:group) +InFolder:/root/fakepath";
    //            Assert.AreEqual(expected, actual);
    //        });
    //    }

    //    [TestMethod]
    //    public async Task Linq_BugReproduction_SnNotSupported_1()
    //    {
    //        const string SystemFolder = "SystemFolder";
    //        const string File = "File";
    //        await LinqTest(repository =>
    //        {
    //            Repository.Root
    //                .CreateChild("TestRoot", SystemFolder, out Node testRoot)
    //                .CreateChild("Folder1", SystemFolder)
    //                .CreateChild("File1", File);
    //            testRoot
    //                .CreateChild("Folder2", SystemFolder)
    //                .CreateChild("File2", File);

    //            var content = repository.Content.DisableAutofilters()
    //                .FirstOrDefault(c => c.InTree(testRoot.Path) && c.Type(File));

    //            Assert.IsNotNull(content);
    //        });
    //    }

    //    [TestMethod]
    //    public async Task Linq_BugReproduction_StartsWith_1()
    //    {
    //        await LinqTest(repository =>
    //        {
    //            // based on original bug report

    //            Assert.AreEqual("Path:/root/ims*",
    //                GetQueryString(repository.Content.Where(c => c.Path.StartsWith("/Root/IMS"))));

    //            // more test cases

    //            Assert.AreEqual("Path:*/mydoc",
    //                GetQueryString(repository.Content.Where(c => c.Path.EndsWith("/MyDoc"))));
    //            Assert.AreEqual("Path:*/views/*",
    //                GetQueryString(repository.Content.Where(c => c.Path.Contains("/Views/"))));

    //        });
    //    }

    /* ================================================================================= */
    [TestMethod]
    public void Linq_ProjectionVisitorFormat()
    {
        var acc = new TypeAccessor(typeof(ProjectionVisitor));
        var expected = new[]
        {
            "0", "first", "second", "third", "4th", "5th", "6th", "7th", "8th", "9th",
            "10th", "11th", "12th", "13th", "14th", "15th", "16th", "17th", "18th", "19th",
            "20th", "21st", "22nd", "23rd", "24th", "25th", "26th", "27th", "28th", "29th",
            "30th", "31st", "32nd", "33rd", "34th", "35th", "36th", "37th", "38th", "39th"
        };
        for (int i = 0; i < 40; i++)
        {
            Assert.AreEqual(expected[i], acc.InvokeStatic("FormatId", i));
        }
    }
    /* ================================================================================= */

    private async Task LinqTest(Action<IRepository> callback)
    {
        var restCaller = CreateRestCallerFor(@"{
  ""d"": {
    ""Id"": 999543,
    ""Name"": ""Content1"",
    ""Type"": ""Content"",
  }
}");

        var repositories = GetRepositoryCollection(services =>
        {
            services.AddSingleton(restCaller);
            services.RegisterGlobalContentType<Content>();
            services.RegisterGlobalContentType<RefTestNode>();
        });
        var repository = await repositories.GetRepositoryAsync(FakeServer, CancellationToken.None)
            .ConfigureAwait(false);

        callback(repository);
    }
    private string GetQueryString<T>(IQueryable<T> queryable)
    {
        var cs = queryable.Provider as ContentSet<T>;
        return cs?.GetCompiledQuery().ToString() ?? string.Empty;
    }
    private QueryContentRequest GetODataRequest<T>(IQueryable<T> queryable)
    {
        var cs = queryable.Provider as ContentSet<T>;
        return cs?.GetODataRequest();
    }

    private async Task LinqExecutionTest(string mockResponse, Action<IRepository> callback)
    {
        var restCaller = CreateRestCallerFor(mockResponse);

        var repositories = GetRepositoryCollection(services =>
        {
            services.AddSingleton(restCaller);
            services.RegisterGlobalContentType<Content>();
            services.RegisterGlobalContentType<RefTestNode>();
        });
        var repository = await repositories.GetRepositoryAsync(FakeServer, CancellationToken.None)
            .ConfigureAwait(false);

        callback(repository);
    }

    private async Task AsEnumerableError(string expectedMethodName, Action<IRepository> action)
    {
        await LinqTest(repository =>
        {
            var message = string.Empty;
            try
            {
                action(repository);
                Assert.Fail("NotSupportedException was not thrown.");
            }
            catch (SnNotSupportedException e)
            {
                message = e.Message;
                // expected exception
            }
            catch (NotSupportedException e)
            {
                message = e.Message;
                // expected exception
            }

            // $"Cannot resolve an expression. Use 'AsEnumerable()' method before calling '{lastMethodName}' method");
            var actualMethodName = message.Split('\'')[3];
            Assert.AreEqual(expectedMethodName, actualMethodName);
        });

    }
    private void InvalidOperationTest(Action action)
    {
        try
        {
            action();
            Assert.Fail("InvalidOperationException was not thrown.");
        }
        catch (InvalidOperationException e)
        {
            // expected exception
        }
    }
    private void ArgumentOutOfRangeTest(Action action)
    {
        try
        {
            action();
            Assert.Fail("ArgumentOutOfRangeException was not thrown.");
        }
        catch (ArgumentOutOfRangeException e)
        {
            // expected exception
        }
    }

}