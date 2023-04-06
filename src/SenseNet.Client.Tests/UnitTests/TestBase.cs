using NSubstitute;

namespace SenseNet.Client.Tests.UnitTests;

public abstract class TestBase
{
    protected IRestCaller CreateRestCallerFor(string returnValueOfGetResponseStringAsync)
    {
        var restCaller = Substitute.For<IRestCaller>();
        restCaller
            .GetResponseStringAsync(Arg.Any<Uri>(), Arg.Any<HttpMethod>(), Arg.Any<string>(), Arg.Any<Dictionary<string, IEnumerable<string>>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(returnValueOfGetResponseStringAsync));
        return restCaller;
    }
}