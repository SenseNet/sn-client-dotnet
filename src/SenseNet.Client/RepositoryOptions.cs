using SenseNet.Client.Authentication;
using SenseNet.Extensions.DependencyInjection;

namespace SenseNet.Client;

/// <summary>
/// Options for connecting to a sensenet repository.
/// </summary>
public class RepositoryOptions
{
    /// <summary>
    /// The URL of the sensenet repository to connect to.
    /// </summary>
    public string Url { get; set; }
    /// <summary>
    /// Authentication options for connecting to the repository.
    /// </summary>
    public AuthenticationOptions Authentication { get; set; } = new AuthenticationOptions();
    /// <summary>
    /// Registered content types. Do not edit this directly, use the dedicated
    /// extension methods for registering content types.
    /// </summary>
    public RegisteredContentTypes RegisteredContentTypes { get; set; }
}