using System.Threading.Tasks;
using System.Threading;

// ReSharper disable once CheckNamespace
namespace SenseNet.Client
{
    /// <summary>
    /// Defines the main client API for managing content items in a sensenet repository.
    /// </summary>
    public interface IRepository
    {
        public ServerContext Server { get; set; }

        /// <summary>
        /// Creates a new content instance in memory.
        /// </summary>
        /// <returns>A new content instance.</returns>
        public Content CreateContent();

        /// <summary>
        /// Loads an existing content.
        /// </summary>
        /// <param name="id">Content id</param>
        /// <param name="cancel">The token to monitor for cancellation requests.</param>
        /// <returns>A task that wraps the content or null.</returns>
        public Task<Content> LoadContentAsync(int id, CancellationToken cancel);
        /// <summary>
        /// Loads an existing content.
        /// </summary>
        /// <param name="path">Content path</param>
        /// <param name="cancel">The token to monitor for cancellation requests.</param>
        /// <returns>A task that wraps the content or null.</returns>
        public Task<Content> LoadContentAsync(string path, CancellationToken cancel);
        /// <summary>
        /// Loads an existing content.
        /// </summary>
        /// <param name="requestData">Detailed request information.</param>
        /// <param name="cancel">The token to monitor for cancellation requests.</param>
        /// <returns>A task that wraps the content or null.</returns>
        public Task<Content> LoadContentAsync(ODataRequest requestData, CancellationToken cancel);
    }
}
