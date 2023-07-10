namespace SenseNet.Client.WebApi
{
    /// <summary>
    /// Defines methods for accessing repositories for dedicated users.
    /// </summary>
    public interface IUserRepositoryCollection
    {
        /// <summary>
        /// Returns the repository that is configured to use the current user's token available in HttpContext.
        /// </summary>
        /// <param name="cancel">The token to monitor for cancellation requests.</param>
        /// <returns>A task that wraps the repository instance.</returns>
        public Task<IRepository> GetUserRepositoryAsync(CancellationToken cancel);
        /// <summary>
        /// Returns a repository without authentication.
        /// </summary>
        /// <param name="cancel">The token to monitor for cancellation requests.</param>
        /// <returns>A task that wraps the repository instance.</returns>
        public Task<IRepository> GetVisitorRepositoryAsync(CancellationToken cancel);
        /// <summary>
        /// Returns a repository that is configured to use the configured Admin user.
        /// </summary>
        /// <param name="cancel">The token to monitor for cancellation requests.</param>
        /// <returns>A task that wraps the repository instance.</returns>
        public Task<IRepository> GetAdminRepositoryAsync(CancellationToken cancel);
    }
}