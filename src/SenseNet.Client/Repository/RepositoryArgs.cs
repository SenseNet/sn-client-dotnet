// ReSharper disable once CheckNamespace
namespace SenseNet.Client
{
    /// <summary>
    /// Defines arguments for constructing a repository instance. All properties are optional.
    /// </summary>
    public class RepositoryArgs
    {
        /// <summary>
        /// Name of a registered repository if you want to access a named repository.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Access token for a certain user. If not provided, the configured authentication
        /// options will be used.
        /// </summary>
        public string AccessToken { get; set; }
    }
}
