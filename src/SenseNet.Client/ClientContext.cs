//using SenseNet.Client.Logging;
using System;

namespace SenseNet.Client
{
    /// <summary>
    /// Holds globally available context properties (e.g. username, urls, etc.). Please fill the
    /// properties of the ClientContext.Current singleton instance.
    /// </summary>
    public class ClientContext
    {
        internal static readonly string TraceCategory = "SnClient.Net";

        //============================================================================= Static API

        private static bool _isInitialized;
        private static readonly ClientContext _context = new ClientContext();

        /// <summary>
        /// A singleton client context instance. Use this for every context-related 
        /// operation (e.g. setting the upload chunk size) after initializing the context.
        /// </summary>
        public static ClientContext Current
        {
            get 
            {
                if (!_isInitialized)
                    throw new ClientException("The system is not initialized. Please call the ClientContext.Initialize method first.");

                return _context; 
            }
        }

        /// <summary>
        /// Initializes the global context instance. You have to call this method
        /// before using the client context.
        /// </summary>
        /// <param name="servers"></param>
        public static void Initialize(ServerContext[] servers)
        {
            if (_isInitialized)
                throw new InvalidOperationException("The client context is already initialized.");
            if (servers == null || servers.Length == 0)
                throw new ArgumentException("Invalid context: please provide at least one server context.");

            _context.Servers = servers;

            // switch this on before accessing the Current property
            _isInitialized = true;
        }

        //============================================================================= Instance API

        /// <summary>
        /// The available servers that can be a target of client operations.
        /// </summary>
        public ServerContext[] Servers { get; private set; }
        /// <summary>
        /// One of the configured servers, chosen randomly.
        /// </summary>
        public ServerContext RandomServer
        {
            get
            {
                if (Servers.Length == 1)
                    return Servers[0];
                return Servers[new Random().Next(0, Servers.Length)];
            }
        }
        
        private int _chunkSizeInBytes = 10485760; // 10 MB
        /// <summary>
        /// Number of bytes sent to the server in one chunk during upload operations. Default: 10 MB.
        /// </summary>
        public int ChunkSizeInBytes 
        { 
            get { return _chunkSizeInBytes; }
            set { _chunkSizeInBytes = value; }
        }
    }
}
