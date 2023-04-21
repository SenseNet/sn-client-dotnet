using System.Threading;
using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace SenseNet.Client
{
    /// <summary>
    /// Defines a repository collection.
    /// </summary>
    /// <remarks>
    /// Use this service as a starting point in your application to acquire instances of registered repositories.
    /// </remarks>
    public interface IRepositoryCollection
    {
        /// <summary>
        /// Returns the unnamed repository.
        /// </summary>
        /// <remarks>This method will return an authenticated repository instance
        /// that can be pinned in the application. This method can be called
        /// multiple times as it caches the repository and will return the
        /// same object.</remarks>
        /// <param name="cancel">The token to monitor for cancellation requests.</param>
        /// <returns>A task that wraps a configured repository instance.</returns>
        public Task<IRepository> GetRepositoryAsync(CancellationToken cancel);

        /// <summary>
        /// Returns a named repository.
        /// </summary>
        /// <remarks>This method will return an authenticated repository instance
        /// that can be pinned in the application. This method can be called
        /// multiple times as it caches the repository and will return the
        /// same object.</remarks>
        /// <param name="name">Name of the repository.</param>
        /// <param name="cancel">The token to monitor for cancellation requests.</param>
        /// <returns>A task that wraps a configured repository instance.</returns>
        public Task<IRepository> GetRepositoryAsync(string name, CancellationToken cancel);

        /// <summary>
        /// Returns a repository defined by the provided arguments.
        /// </summary>
        /// <remarks>This method will return an authenticated repository instance
        /// that can be pinned in the application. This method can be called
        /// multiple times as it caches the repository and will return the
        /// same object.</remarks>
        /// <param name="repositoryArgs">Repository arguments. If you provide a user-specific token,
        /// you will be able to access the repository in the name of that user.</param>
        /// <param name="cancel">The token to monitor for cancellation requests.</param>
        /// <returns>A task that wraps a configured repository instance.</returns>
        public Task<IRepository> GetRepositoryAsync(RepositoryArgs repositoryArgs, CancellationToken cancel);
    }
}
