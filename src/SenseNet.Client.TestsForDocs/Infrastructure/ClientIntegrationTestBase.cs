using System;
using System.Diagnostics;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Client.Authentication;
using SenseNet.Diagnostics;
using SenseNet.Extensions.DependencyInjection;
// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract

namespace SenseNet.Client.TestsForDocs.Infrastructure
{
    //UNDONE:- Feature request: find a solution for the tests to get a raw response.
    //UNDONE:- Feature request: find a solution for the tests to get a final request url instead of making the request.
    [TestClass]
    public class ClientIntegrationTestBase
    {
        public static readonly string Url = "https://localhost:44362";

        [AssemblyInitialize]
        public static void InitializeAllTests(TestContext context)
        {
            var repository = InitServer(context);
            var cancel = new CancellationToken();

            EnsureBasicStructureAsync(repository, cancel).ConfigureAwait(false).GetAwaiter().GetResult();

            SnTrace.Custom.Enabled = true;
            SnTrace.Test.Enabled = true;
        }

        protected static readonly string CarContentType = @"<?xml version='1.0' encoding='utf-8'?>
<ContentType name='Car' parentType='GenericContent' handler='SenseNet.ContentRepository.GenericContent' xmlns='http://schemas.sensenet.com/SenseNet/ContentRepository/ContentTypeDefinition'>
  <DisplayName>Car</DisplayName>
  <Description>Car</Description>
  <Icon>Car</Icon>
  <AllowIncrementalNaming>true</AllowIncrementalNaming>
  <Fields>
    <Field name='Name' type='ShortText'/>
    <Field name='Make' type='ShortText'/>
    <Field name='Model' type='ShortText'/>
    <Field name='Style' type='Choice'>
      <Configuration>
        <AllowMultiple>false</AllowMultiple>
        <AllowExtraValue>true</AllowExtraValue>
        <Options>
          <Option value='Sedan' selected='true'>Sedan</Option>
          <Option value='Coupe'>Coupe</Option>
          <Option value='Cabrio'>Cabrio</Option>
          <Option value='Roadster'>Roadster</Option>
          <Option value='SUV'>SUV</Option>
          <Option value='Van'>Van</Option>
        </Options>
      </Configuration>
    </Field>
    <Field name='StartingDate' type='DateTime'/>
    <Field name='Color' type='ShortText'/>
    <Field name='EngineSize' type='ShortText'/>
    <Field name='Power' type='ShortText'/>
    <Field name='Price' type='Number'/>
  </Fields>
</ContentType>
";
        protected static readonly string ArticleContentType = @"<?xml version='1.0' encoding='utf-8'?>
<ContentType name='Article' parentType='GenericContent' handler='SenseNet.ContentRepository.GenericContent' xmlns='http://schemas.sensenet.com/SenseNet/ContentRepository/ContentTypeDefinition'>
  <DisplayName>Article</DisplayName>
  <Description>Article</Description>
  <Fields>
    <Field name='Lead' type='LongText'/>
    <Field name='Body' type='LongText'/>
  </Fields>
</ContentType>
";
        protected static async Task EnsureBasicStructureAsync(IRepository repository, CancellationToken cancel)
        {
            var carCt = await repository.LoadContentAsync<ContentType>("/Root/System/Schema/ContentTypes/GenericContent/Car", cancel);
            if (carCt == null)
            {
                await repository.UploadAsync(new UploadRequest
                {
                    FileName = "Car",
                    ContentName = "Car",
                    ContentType = "ContentType",
                    ParentPath = "/Root/System/Schema/ContentTypes/GenericContent"
                }, CarContentType, cancel);
            }
            var articleCt = await repository.LoadContentAsync<ContentType>("/Root/System/Schema/ContentTypes/GenericContent/Article", cancel);
            if (articleCt == null)
            {
                await repository.UploadAsync(new UploadRequest
                {
                    FileName = "Article",
                    ContentName = "Article",
                    ContentType = "ContentType",
                    ParentPath = "/Root/System/Schema/ContentTypes/GenericContent"
                }, ArticleContentType, cancel);
            }

            await repository.InvokeActionAsync(new OperationRequest
            {
                Path = "/Root/Content",
                OperationName = "AddAllowedChildTypes",
                Parameters = { { "contentTypes", "Car" } }
            }, cancel);
            await repository.InvokeActionAsync(new OperationRequest
            {
                Path = "/Root/Content",
                OperationName = "AddAllowedChildTypes",
                Parameters = { { "contentTypes", "Article" } }
            }, cancel);

            await EnsureContentAsync("/Root/Content", "Folder", repository, cancel);
            //await EnsureContentAsync("/Root/Content/IT", "Workspace", repository, cancel);
            //await EnsureContentAsync("/Root/Content/IT/Document_Library", "DocumentLibrary", c =>
            //{
            //    c["Description"] = "Document library of IT";
            //}, repository, cancel);
            //await EnsureContentAsync("/Root/Content/IT/Document_Library/Chicago", "Folder", repository, cancel);
            //await EnsureContentAsync("/Root/Content/IT/Document_Library/Calgary", "Folder", repository, cancel);
            //await EnsureContentAsync("/Root/Content/IT/Document_Library/Calgary/BusinessPlan.docx", "File", repository, cancel);
            //await EnsureContentAsync("/Root/Content/IT/Document_Library/Munich", "Folder", repository, cancel);
            await EnsureContentAsync("/Root/IMS/Public", "Domain", repository, cancel);
            await EnsureContentAsync("/Root/IMS/Public/Editors", "Group", repository, cancel);
            await EnsureContentAsync("/Root/IMS/Public/etaylor", "User", c =>
            {
                c["LoginName"] = "etaylor";
                c["Email"] = "etaylor@example.com";
                c["Password"] = "sYYsdqPVnuOg5YPb8Rkg";
                c["FullName"] = "Emma Taylor";
                c["Enabled"] = true;
            }, repository, cancel);
            await EnsureContentAsync("/Root/IMS/Public/jjohnson", "User", c =>
            {
                c["LoginName"] = "jjohnson";
                c["Email"] = "jjohnson@example.com";
                c["Password"] = "QJjJdNKY8ejZ1rUJWFsf";
                c["FullName"] = "James Johnson";
                c["Enabled"] = true;
            }, repository, cancel);

            await EnsureContentAsync("/Root/Content/Documents", "DocumentLibrary", repository, cancel);

            await EnsureContentAsync("/Root/Content/Cars", "Folder", c =>
            {
                c["Description"] = "This folder contains our cars.";
            }, repository, cancel);
            await EnsureContentAsync("/Root/Content/Cars/out-of-order", "Folder", c =>
            {
                c["DisplayName"] = "Out of order";
            }, repository, cancel);
            await EnsureContentAsync("/Root/Content/Cars/Settings", "SystemFolder", c =>
            {
                c["DisplayName"] = "Settings";
            }, repository, cancel);
            await EnsureContentAsync("/Root/Content/Cars/Backup", "SystemFolder", c =>
            {
                c["DisplayName"] = "Backup";
            }, repository, cancel);

            await EnsureContentAsync("/Root/Content/Cars/OT1234", "Car", c =>
            {
                c["DisplayName"] = "Fiat 126";
                c["Make"] = "Fiat";
                c["Model"] = "126";
                c["Style"] = "Coupe";
                c["StartingDate"] = DateTime.Parse("1986-11-20");
                c["Color"] = "Yellow";
                c["EngineSize"] = "600 ccm";
                c["Power"] = "21 hp";
                c["Price"] = 120_000;
            }, repository, cancel);
            await EnsureContentAsync("/Root/Content/Cars/AADF953", "Car", c =>
            {
                c["DisplayName"] = "Opel Astra H";
                c["Make"] = "Opel";
                c["Model"] = "Astra H";
                c["Style"] = "Coupe";
                c["StartingDate"] = DateTime.Parse("2010-06-10");
                c["Color"] = "Gray";
                c["EngineSize"] = "1600 ccm";
                c["Power"] = "120 hp";
                c["Price"] = 1_200_000;
            }, repository, cancel);
            await EnsureContentAsync("/Root/Content/Cars/AASE642", "Car", c =>
            {
                c["DisplayName"] = "Skoda Octavia";
                c["Make"] = "Skoda";
                c["Model"] = "Octavia";
                c["Style"] = "Coupe";
                c["StartingDate"] = DateTime.Parse("2021-04-22");
                c["Color"] = "White";
                c["EngineSize"] = "1400 ccm";
                c["Power"] = "140 hp";
                c["Price"] = 8_350_000;
            }, repository, cancel);
            await EnsureContentAsync("/Root/Content/Cars/AAKE452", "Car", c =>
            {
                c["DisplayName"] = "Mazda 6";
                c["Make"] = "Mazda";
                c["Model"] = "6";
                c["Style"] = "Coupe";
                c["StartingDate"] = DateTime.Parse("2021-08-28");
                c["Color"] = "Red";
                c["EngineSize"] = "1800 ccm";
                c["Power"] = "130 hp";
                c["Price"] = 7_850_000;
            }, repository, cancel);
            await EnsureContentAsync("/Root/Content/Cars/OT6578", "Car", c =>
            {
                c["DisplayName"] = "Toyota AE86";
                c["Make"] = "Toyota";
                c["Model"] = "AE86";
                c["Style"] = "Sedan";
                c["StartingDate"] = DateTime.Parse("1981-05-26");
                c["Color"] = "White";
                c["EngineSize"] = "1600 ccm";
                c["Power"] = "190 hp";
                c["Price"] = 16_900_000;
            }, repository, cancel);
            await EnsureContentAsync("/Root/Content/Cars/JRT5698", "Car", c =>
            {
                c["DisplayName"] = "Toyota Supra MK4";
                c["Make"] = "Toyota";
                c["Model"] = "Supra MK4";
                c["Style"] = "Coupe";
                c["StartingDate"] = DateTime.Parse("1999-04-27");
                c["Color"] = "Red";
                c["EngineSize"] = "3000 ccm";
                c["Power"] = "320 hp";
                c["Price"] = 18_600_000;
            }, repository, cancel);
            await EnsureContentAsync("/Root/Content/Cars/KLT1159", "Car", c =>
            {
                c["DisplayName"] = "Renault Thalia";
                c["Make"] = "Renault";
                c["Model"] = "Thalia";
                c["Style"] = "Coupe";
                c["StartingDate"] = DateTime.Parse("2013-09-11");
                c["Color"] = "Green";
                c["EngineSize"] = "1400 ccm";
                c["Power"] = "105 hp";
                c["Price"] = 4_930_000;
            }, repository, cancel);
            await EnsureContentAsync("/Root/Content/Cars/GLW1814", "Car", c =>
            {
                c["DisplayName"] = "Suzuki Swift";
                c["Make"] = "Suzuki";
                c["Model"] = "Swift";
                c["Style"] = "Coupe";
                c["StartingDate"] = DateTime.Parse("2006-01-10");
                c["Color"] = "Brown";
                c["EngineSize"] = "900 ccm";
                c["Power"] = "90 hp";
                c["Price"] = 2_240_000;
            }, repository, cancel);
            await EnsureContentAsync("/Root/Content/Cars/AACE642", "Car", c =>
            {
                c["DisplayName"] = "Nissan GTR R32";
                c["Make"] = "Nissan";
                c["Model"] = "GTR R32";
                c["Style"] = "Coupe";
                c["StartingDate"] = DateTime.Parse("2023-12-29 09:30:00");
                c["Color"] = "Black";
                c["EngineSize"] = "2800 ccm";
                c["Power"] = "320 hp";
                c["Price"] = 38_000_000;
            }, repository, cancel);
            await EnsureContentAsync("/Root/Content/Cars/AAXX123", "Car", c =>
            {
                c["DisplayName"] = "Ferrari California";
                c["Make"] = "Ferrari";
                c["Model"] = "California";
                c["Style"] = "Roadster";
                c["StartingDate"] = DateTime.Parse("2019-03-14");
                c["Color"] = "Nero Daytona";
                c["EngineSize"] = "4.3 l";
                c["Power"] = "454 hp";
                c["Price"] = 60_000_000;
            }, repository, cancel);
        }

        public TestContext TestContext { get; set; }

        [TestInitialize]
        public void InitializeTest()
        {
            SnTrace.Test.Write($">>>> TEST: {TestContext.TestName}");
        }

        private static IRepository InitServer(TestContext context)
        {
            // workaround for authenticating using the configured client id and secret
            var config = new ConfigurationBuilder()
                .SetBasePath(context.DeploymentDirectory)
                .AddJsonFile("appsettings.json", optional: true)
                .AddUserSecrets<ClientIntegrationTestBase>()
                .Build();

            // create a service collection and register the sensenet client
            var services = new ServiceCollection()
                .AddSenseNetClient()
                .ConfigureSenseNetRepository(repositoryOptions =>
                {
                    config.GetSection("sensenet:repository").Bind(repositoryOptions);
                })
                .BuildServiceProvider();

            // get the repository amd extract the server context
            var repositories = services.GetRequiredService<IRepositoryCollection>();
            var repository = repositories.GetRepositoryAsync(CancellationToken.None).GetAwaiter().GetResult();
            
            var server = repository.Server;

            var ctx = ClientContext.Current;
            ctx.RemoveServers(ctx.Servers);
            ctx.AddServer(server);

            return repository;
        }

        protected static Task<Content> EnsureContentAsync(string path, string typeName, IRepository repository, CancellationToken cancel)
        {
            return EnsureContentAsync(path, typeName, null, repository, cancel);
        }
        protected static async Task<Content> EnsureContentAsync(string path, string typeName, Action<Content>? setProperties, IRepository repository, CancellationToken cancel)
        {
            var content = await repository.LoadContentAsync(path, cancel);
            if (content == null)
            {
                var parentPath = RepositoryPath.GetParentPath(path);
                var name = RepositoryPath.GetFileName(path);
                content = repository.CreateContent(parentPath, typeName, name);
                if (setProperties == null)
                {
                    await content.SaveAsync(cancel);
                    return content;
                }
            }

            if (setProperties != null)
            {
                setProperties(content);
                await content.SaveAsync(cancel);
            }

            return content;
        }

        protected IRepositoryCollection GetRepositoryCollection(Action<IServiceCollection>? addServices = null)
        {
            var services = new ServiceCollection();

            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Trace);
            });

            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true)
                .AddUserSecrets<ClientIntegrationTestBase>()
                .Build();

            services
                .AddSingleton<IConfiguration>(config)
                .AddSenseNetClient()
                //.AddSingleton<ITokenProvider, TestTokenProvider>()
                //.AddSingleton<ITokenStore, TokenStore>()
                .ConfigureSenseNetRepository("local", repositoryOptions =>
                {
                    // set test url and authentication in user secret
                    config.GetSection("sensenet:repository").Bind(repositoryOptions);
                });

            addServices?.Invoke(services);

            var provider = services.BuildServiceProvider();
            return provider.GetRequiredService<IRepositoryCollection>();
        }

    }
}
