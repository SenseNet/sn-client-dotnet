using SenseNet.Client.Authentication;
using SenseNet.Extensions.DependencyInjection;

namespace SenseNet.Client;

public class RepositoryOptions
{
    public string Url { get; set; }
    public AuthenticationOptions Authentication { get; set; } = new AuthenticationOptions();
    public RegisteredContentTypes RegisteredContentTypes { get; set; }
}