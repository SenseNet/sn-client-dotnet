namespace SenseNet.Client.Authentication
{
    /// <summary>
    /// Options for authenticating with a sensenet repository.
    /// </summary>
    public class AuthenticationOptions
    {
        /// <summary>
        /// The client id to use for authentication. If you set the client id,
        /// you must also set the client secret. Alternatively you can use an API key.
        /// </summary>
        public string ClientId { get; set; }
        /// <summary>
        /// The client secret to use for authentication.
        /// </summary>
        public string ClientSecret { get; set; }
        /// <summary>
        /// The API key to use for authentication. This key in itself is enough to authenticate
        /// with the repository. If you set the API key, you don't need to set the client id and secret.
        /// </summary>
        public string ApiKey { get; set; }
    }
}
