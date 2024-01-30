using Microsoft.Extensions.Logging;
using System;

// ReSharper disable once CheckNamespace
namespace SenseNet.Client
{
    /// <summary>
    /// Represents an image in the sensenet repository.
    /// </summary>
    public class Image : File
    {
        public int? Width { get; set; }
        public int? Height { get; set; }
        public DateTime? DateTaken { get; set; }
        public string? Keywords { get; set; }

        public Image(IRestCaller restCaller, ILogger<Image> logger) : base(restCaller, logger)
        {
        }
    }
}