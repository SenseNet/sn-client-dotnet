namespace SenseNet.Client
{
    /// <summary>
    /// Represents a connection to a server.
    /// </summary>
    public class ServerContext
    {
        /// <summary>
        /// Username used for authenticating all requests made to this server.
        /// In case of an empty username DefaultCredentials will be used.
        /// </summary>
        public string Username { get; set; }
        /// <summary>
        /// Password for the username.
        /// </summary>
        public string Password { get; set; }
        /// <summary>
        /// Server URL.
        /// </summary>
        public string Url { get; set; }

        //============================================================================= Static API

        /// <summary>
        /// Gets a URL from a server instance for sending requests. In case of a null
        /// instance the first one from the currently configured list will be used.
        /// </summary>
        /// <param name="server">Server context object.</param>
        public static string GetUrl(ServerContext server)
        {
            if (server == null)
                server = ClientContext.Current.Server;
            return server.Url;
        }
    }
}
