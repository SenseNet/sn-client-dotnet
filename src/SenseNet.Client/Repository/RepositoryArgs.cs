// ReSharper disable once CheckNamespace
namespace SenseNet.Client
{
    /// <summary>
    /// Defines parameters for a repository instance.
    /// </summary>
    public class RepositoryArgs
    {
        /// <summary>
        /// Name of the repository. Use the same name as during registration or
        /// leave it empty to load the default repository.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Access token for a certain user. If not provided, the configured authentication
        /// options will be used.
        /// </summary>
        public string AccessToken { get; set; }
    }
}
