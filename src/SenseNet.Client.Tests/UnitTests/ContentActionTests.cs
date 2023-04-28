using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using SenseNet.Extensions.DependencyInjection;

namespace SenseNet.Client.Tests.UnitTests;

[TestClass]
public class ContentActionTests : TestBase
{
    [TestMethod]
    public async Task ContentAction_CopyTo()
    {
        await ActionTest(
            expectedUrlPart: "OData.svc/('Root')/CopyBatch?",
            expectedPostData: "models=[{\"paths\":[42],\"targetPath\":\"/Root/Content/CopyTarget\"}]",
            callback: async (content, cancel) =>
            {
                await content.CopyToAsync("/Root/Content/CopyTarget", cancel);
            }).ConfigureAwait(false);
    }
    [TestMethod]
    public async Task ContentAction_CopyTo_WithoutId()
    {
        await ActionTest(
            expectedUrlPart: "OData.svc/('Root')/CopyBatch?",
            expectedPostData: "models=[{\"paths\":[\"/Root/Content/MyContent\"],\"targetPath\":\"/Root/Content/CopyTarget\"}]",
            callback: async (content, cancel) =>
            {
                content.Id = 0;
                await content.CopyToAsync("/Root/Content/CopyTarget", cancel);
            }).ConfigureAwait(false);
    }
    [TestMethod]
    public async Task ContentAction_CopyTo_Error_MissingTargetPath()
    {
        await ActionErrorTest(
            expectedExceptionType: typeof(ArgumentNullException),
            expectedMessage: "Value cannot be null. (Parameter 'targetPath')",
            callback: async (content, cancel) =>
            {
                await content.CopyToAsync(null, cancel);
            });
    }
    [TestMethod]
    public async Task ContentAction_CopyTo_Error_WithoutIdAndPath()
    {
        await ActionErrorTest(
            expectedExceptionType: typeof(InvalidOperationException),
            expectedMessage: "Cannot execute 'CopyTo' action of a Content if neither Id and Path provided.",
            callback: async (content, cancel) =>
            {
                content.Id = 0;
                content.Path = null;
                await content.CopyToAsync("/Root/Content/CopyTarget", cancel);
            });
    }

    [TestMethod]
    public async Task ContentAction_MoveTo()
    {
        await ActionTest(
            expectedUrlPart: "OData.svc/('Root')/MoveBatch?",
            expectedPostData: "models=[{\"paths\":[42],\"targetPath\":\"/Root/Content/MoveTarget\"}]",
            callback: async (content, cancel) =>
            {
                await content.MoveToAsync("/Root/Content/MoveTarget", cancel);
            }).ConfigureAwait(false);
    }
    [TestMethod]
    public async Task ContentAction_MoveTo_WithoutId()
    {
        await ActionTest(
            expectedUrlPart: "OData.svc/('Root')/MoveBatch?",
            expectedPostData: "models=[{\"paths\":[\"/Root/Content/MyContent\"],\"targetPath\":\"/Root/Content/MoveTarget\"}]",
            callback: async (content, cancel) =>
            {
                content.Id = 0;
                await content.MoveToAsync("/Root/Content/MoveTarget", cancel);
            }).ConfigureAwait(false);
    }
    [TestMethod]
    public async Task ContentAction_MoveTo_Error_MissingTargetPath()
    {
        await ActionErrorTest(
            expectedExceptionType: typeof(ArgumentNullException),
            expectedMessage: "Value cannot be null. (Parameter 'targetPath')",
            callback: async (content, cancel) =>
            {
                await content.MoveToAsync(null, cancel);
            });
    }
    [TestMethod]
    public async Task ContentAction_MoveTo_Error_WithoutIdAndPath()
    {
        await ActionErrorTest(
            expectedExceptionType: typeof(InvalidOperationException),
            expectedMessage: "Cannot execute 'MoveTo' action of a Content if neither Id and Path provided.",
            callback: async (content, cancel) =>
            {
                content.Id = 0;
                content.Path = null;
                await content.MoveToAsync("/Root/Content/CopyTarget", cancel);
            });
    }

    [TestMethod]
    public async Task ContentAction_CheckOut()
    {
        await ActionTest(
            expectedUrlPart: "OData.svc/content(42)/CheckOut?",
            expectedPostData: "models=[null]",
            callback: async (content, cancel) =>
            {
                await content.CheckOutAsync(cancel);
            }).ConfigureAwait(false);
    }
    [TestMethod]
    public async Task ContentAction_CheckOut_WithoutId()
    {
        await ActionTest(
            expectedUrlPart: "OData.svc/Root/Content('MyContent')/CheckOut?",
            expectedPostData: "models=[null]",
            callback: async (content, cancel) =>
            {
                content.Id = 0;
                await content.CheckOutAsync(cancel);
            }).ConfigureAwait(false);
    }
    [TestMethod]
    public async Task ContentAction_CheckOut_Error_WithoutIdAndPath()
    {
        await ActionErrorTest(
            expectedExceptionType: typeof(InvalidOperationException),
            expectedMessage: "Cannot execute 'CheckOut' action of a Content if neither Id and Path provided.",
            callback: async (content, cancel) =>
            {
                content.Id = 0;
                content.Path = null;
                await content.CheckOutAsync(cancel);
            });
    }

    [TestMethod]
    public async Task ContentAction_UndoCheckOut()
    {
        await ActionTest(
            expectedUrlPart: "OData.svc/content(42)/UndoCheckOut?",
            expectedPostData: "models=[null]",
            callback: async (content, cancel) =>
            {
                await content.UndoCheckOutAsync(cancel);
            }).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ContentAction_ForceUndoCheckOut()
    {
        await ActionTest(
            expectedUrlPart: "OData.svc/content(42)/ForceUndoCheckOut?",
            expectedPostData: "models=[null]",
            callback: async (content, cancel) =>
            {
                await content.ForceUndoCheckOutAsync(cancel);
            }).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ContentAction_CheckIn()
    {
        await ActionTest(
            expectedUrlPart: "OData.svc/content(42)/CheckIn?",
            expectedPostData: "models=[null]",
            callback: async (content, cancel) =>
            {
                await content.CheckInAsync(cancel);
            }).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ContentAction_Publish()
    {
        await ActionTest(
            expectedUrlPart: "OData.svc/content(42)/Publish?",
            expectedPostData: "models=[null]",
            callback: async (content, cancel) =>
            {
                await content.PublishAsync(cancel);
            }).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ContentAction_Reject_WithoutReason()
    {
        await ActionTest(
            expectedUrlPart: "OData.svc/content(42)/Reject?",
            expectedPostData: "models=[null]",
            callback: async (content, cancel) =>
            {
                await content.RejectAsync(null, cancel);
            }).ConfigureAwait(false);
    }
    [TestMethod]
    public async Task ContentAction_Reject_WithReason()
    {
        await ActionTest(
            expectedUrlPart: "OData.svc/content(42)/Reject?",
            expectedPostData: "models=[{\"rejectReason\":\"Reject reason.\"}]",
            callback: async (content, cancel) =>
            {
                await content.RejectAsync("Reject reason.", cancel);
            }).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ContentAction_Approve()
    {
        await ActionTest(
            expectedUrlPart: "OData.svc/content(42)/Approve?",
            expectedPostData: "models=[null]",
            callback: async (content, cancel) =>
            {
                await content.ApproveAsync(cancel);
            }).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ContentAction_RestoreVersion()
    {
        await ActionTest(
            expectedUrlPart: "OData.svc/content(42)/RestoreVersion?",
            expectedPostData: "models=[{\"version\":\"V1.0\"}]",
            callback: async (content, cancel) =>
            {
                await content.RestoreVersionAsync("V1.0", cancel);
            }).ConfigureAwait(false);
    }
    [TestMethod]
    public async Task ContentAction_RestoreVersion_MissingVersion()
    {
        await ActionErrorTest(
            expectedExceptionType: typeof(ArgumentNullException),
            expectedMessage: "Value cannot be null. (Parameter 'version')",
            callback: async (content, cancel) =>
            {
                await content.RestoreVersionAsync(null, cancel);
            }).ConfigureAwait(false);
    }

    /* ===================================================================== TOOLS */

    private async Task ActionTest(string expectedUrlPart, string expectedPostData, Func<Content, CancellationToken, Task> callback)
    {
        var cancel = new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token;
        var restCaller = Substitute.For<IRestCaller>();
        var repositories = GetRepositoryCollection(services =>
        {
            services.AddSingleton(restCaller);
        });
        var repository = await repositories.GetRepositoryAsync("local", CancellationToken.None)
            .ConfigureAwait(false);

        var content = repository.CreateExistingContent(42);
        content.Path = "/Root/Content/MyContent";

        // ACT
        await callback(content, cancel);

        // ASSERT
        var calls = restCaller.ReceivedCalls().ToArray();
        Assert.IsNotNull(calls);
        Assert.AreEqual(2, calls.Length);
        Assert.AreEqual("GetResponseStringAsync", calls[1].GetMethodInfo().Name);
        var arguments = calls[1].GetArguments();
        Assert.IsTrue(arguments[0]?.ToString()?.Contains(expectedUrlPart));
        Assert.AreEqual(HttpMethod.Post, arguments[1]);
        var json = (string)arguments[2]!;
        Assert.AreEqual(expectedPostData, json);
    }

    private async Task ActionErrorTest(Type expectedExceptionType, string expectedMessage,
        Func<Content, CancellationToken, Task> callback)
    {
        var cancel = new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token;
        var repository = await GetRepositoryCollection()
            .GetRepositoryAsync("local", CancellationToken.None).ConfigureAwait(false);

        var content = repository.CreateExistingContent(42);
        content.Path = "/Root/Content/MyContent";

        // ACT
        Exception? exception = null;
        try
        {
            await callback(content, cancel).ConfigureAwait(false);
            Assert.Fail("Exception was not thrown.");
        }
        catch (Exception e)
        {
            exception = e;
        }

        // ASSERT
        Assert.IsInstanceOfType(exception, expectedExceptionType);
        Assert.AreEqual(expectedMessage, exception.Message);
    }
}