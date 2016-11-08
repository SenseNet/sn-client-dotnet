using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace SenseNet.Client
{
    /// <summary>
    /// Offers extension methods for client Content operations.
    /// </summary>
    public static class ContentExtensions
    {
        /// <summary>
        /// Converts a dynamic collection to a collection of client Content.
        /// </summary>
        /// <param name="source">List of dynamic items (e.g a reference field value in a content JSON) that can be coverted to Content items.</param>
        /// <param name="server">Optional server argument for content items.</param>
        public static IEnumerable<Content> ToContentEnumerable(this IEnumerable<dynamic> source, ServerContext server = null)
        {
            return source.Select(rc => (Content)Content.CreateFromResponse(rc, server));
        }

        /// <summary>
        /// Converts a JArray to a collection of client Content.
        /// </summary>
        /// <param name="source">Array of JTokens (e.g a reference field value in a content JSON) that can be coverted to Content items.</param>
        /// <param name="server">Optional server argument for content items.</param>
        public static IEnumerable<Content> ToContentEnumerable(this JArray source, ServerContext server = null)
        {
            return source.Select(rc => Content.CreateFromResponse(rc, server));
        }
    }
}
