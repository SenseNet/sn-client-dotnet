using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Client.TestsForDocs.Infrastructure;
using System.Threading.Channels;
using SenseNet.Extensions.DependencyInjection;
using SenseNet.Diagnostics;

namespace SenseNet.Client.TestsForDocs;

[TestClass]
public class Settings : ClientIntegrationTestBase
{
    // ReSharper disable once InconsistentNaming
    private CancellationToken cancel => new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token;
    // ReSharper disable once InconsistentNaming
    IRepository repository => GetRepositoryCollection().GetRepositoryAsync("local", cancel)
        .GetAwaiter().GetResult();

    /* ====================================================================================== ???? */

    /// <tab category="settings" article="setings" example="read" />
    [TestMethod]
    [Description("Reading settings")]
    public async Task Docs_Settings_Read()
    {
        //UNDONE:Docs2: not implemented
        Assert.Inconclusive();
        SnTrace.Test.Write(">>>> ACT");
        /*<doc>*/
        /*</doc>*/
        SnTrace.Test.Write(">>>> ACT END");



        await repository.UploadAsync(new UploadRequest
        {
            ParentPath = "/Root/System/Settings",
            ContentName = "MySettings.settings",
            ContentType = "Settings"
        }, "{\"P1\":\"V1\"}", cancel);
        // Ensure empty settings because an older test run can leave noisy data.
        await repository.GetResponseStringAsync(new ODataRequest
        {
            Path = "/Root/Content",
            ActionName = "WriteSettings",
            PostData = new
            {
                name = "MySettings",
                settingsData = new { }
            }
        }, HttpMethod.Post, cancel);

        // ACT
        /*<doc>*/
        dynamic settings = await repository.GetResponseJsonAsync(new ODataRequest
        {
            Path = "/Root/Content/IT",
            ActionName = "GetSettings",
            Parameters = { {"name", "MySettings" } }
        }, HttpMethod.Get, cancel);
        /*</doc>*/

        // ASSERT
        Assert.IsNotNull(settings);
        Assert.AreEqual("V1", settings.P1.ToString());
    }

    /// <tab category="settings" article="setings" example="write" />
    [TestMethod]
    [Description("Writing settings")]
    public async Task Docs_Settings_Write()
    {
        //UNDONE:Docs2: not implemented
        Assert.Inconclusive();
        SnTrace.Test.Write(">>>> ACT");
        /*<doc>*/
        /*</doc>*/
        SnTrace.Test.Write(">>>> ACT END");



        await repository.UploadAsync(new UploadRequest
        {
            ParentPath = "/Root/System/Settings",
            ContentName = "MySettings.settings",
            ContentType = "Settings"
        }, "{\"P1\":\"V1\"}", cancel);

        // ACT
        /*<doc>*/
        var response = await repository.GetResponseStringAsync(new ODataRequest
        {
            Path = "/Root/Content",
            ActionName = "WriteSettings",
            PostData = new
            {
                name = "MySettings",
                settingsData = new { P1 = "V11"}
            }
        }, HttpMethod.Post, cancel);
        /*</doc>*/

        // ASSERT
        dynamic settings = await repository.GetResponseJsonAsync(new ODataRequest
        {
            Path = "/Root/Content/IT",
            ActionName = "GetSettings",
            Parameters = { { "name", "MySettings" } }
        }, HttpMethod.Get, cancel);
        Assert.IsNotNull(settings);
        Assert.AreEqual("V11", settings.P1.ToString());
    }

}