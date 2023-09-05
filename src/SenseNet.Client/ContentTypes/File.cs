using Microsoft.Extensions.Logging;
// ReSharper disable CheckNamespace

namespace SenseNet.Client
{
    /// <summary>
    /// Represents a file in the sensenet repository.
    /// </summary>
    public class File : Content
    {
        public File(IRestCaller restCaller, ILogger<File> logger) : base(restCaller, logger)
        {
        }
    }
}
