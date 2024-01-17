using Microsoft.Extensions.Logging;

// ReSharper disable once CheckNamespace
namespace SenseNet.Client;

public class Settings : File
{
    public bool? GlobalOnly { get; set; }

    public Settings(IRestCaller restCaller, ILogger<File> logger) : base(restCaller, logger) { }
}