using System.Threading;
using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace SenseNet.Client
{
    /// <summary>
    /// Defines a repository collection.
    /// </summary>
    public interface IRepositoryCollection
    {
        /// <summary>
        /// Returns the unnamed repository.
        /// </summary>
        /// <param name="cancel">The token to monitor for cancellation requests.</param>
        /// <returns>A task that wraps a configured repository instance.</returns>
        public Task<IRepository> GetRepositoryAsync(CancellationToken cancel);

        /// <summary>
        /// Returns a named repository.
        /// </summary>
        /// <remarks>
        /// 
        /// </remarks>
        /// <param name="name">Name of the repository.</param>
        /// <param name="cancel">The token to monitor for cancellation requests.</param>
        /// <returns>A task that wraps a configured repository instance.</returns>
        public Task<IRepository> GetRepositoryAsync(string name, CancellationToken cancel);
    }
}
