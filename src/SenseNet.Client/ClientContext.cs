using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

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

        /// <summary>
        /// A singleton client context instance. Use this for every context-related 
        /// operation (e.g. setting the upload chunk size).
        /// </summary>
        public static ClientContext Current { get; internal set; } = new ClientContext();

        /// <summary>
        /// Obsolete method for initializing the client environment. Use the ClientContext.Current property instead.
        /// </summary>
        /// <param name="servers"></param>
        [Obsolete("Initializing the client is not necessary anymore. Add servers using the Current singleton property instead.")]
        public static void Initialize(ServerContext[] servers)
        {
            if (servers != null)
                Current.Servers = servers;
        }

        //============================================================================= Instance API

        private readonly object _serversLock = new object();

        // Store a thread-local random-generator object to avoid generating the same
        // random number multiple times but ensure thread safety.
        // Study material: http://dilbert.com/strip/2001-10-25
        private static int _randomSeed = Environment.TickCount;
        private static readonly ThreadLocal<Random> RandomGenerator = new ThreadLocal<Random>(() => 
            new Random(Interlocked.Increment(ref _randomSeed)));

        /// <summary>
        /// The available servers that can be a target of client operations.
        /// </summary>
        public ServerContext[] Servers { get; private set; } = new ServerContext[0];
        /// <summary>
        /// Returns the first server from the list (a shortcut for Servers[0]). Throws
        /// an exception if there are no servers available.
        /// </summary>
        public ServerContext Server
        {
            get
            {
                // store the list in a local variable for thread safety
                var servers = Servers;
                if (servers.Length < 1)
                    throw new InvalidOperationException("Please add at least one Server before using the client.");

                return servers[0];
            }
        }
        /// <summary>
        /// One of the configured servers, chosen randomly.
        /// </summary>
        public ServerContext RandomServer
        {
            get
            {
                // store the list in a local variable for thread safety
                var servers = Servers;
                return servers.Length < 2 ? Server : servers[RandomGenerator.Value.Next(0, servers.Length)];
            }
        }

        /// <summary>
        /// Adds a single server to the servers list. This method is thread safe.
        /// </summary>
        /// <param name="server">A server context where the client will send requests.</param>
        public void AddServer(ServerContext server)
        {
            AddServers(new[] { server });
        }
        /// <summary>
        /// Adds multiple servers to the servers list. This method is thread safe.
        /// </summary>
        /// <param name="servers">A list of server contexts where the client will send requests.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public void AddServers(IEnumerable<ServerContext> servers)
        {
            if (servers == null)
                throw new ArgumentNullException(nameof(servers));

            lock (_serversLock)
            {
                var serverList = new List<ServerContext>(Servers);

                // this algorithm compares server objects by _reference_, an that is intentional
                serverList.AddRange(servers.Where(s => s != null && !serverList.Contains(s)));

                Servers = serverList.ToArray();
            }
        }
        /// <summary>
        /// Removes a server from the servers list. This method is thread safe.
        /// </summary>
        /// <param name="server">A server context to remove.</param>
        public void RemoveServer(ServerContext server)
        {
            RemoveServers(new []{ server });
        }
        /// <summary>
        /// Removes multiple servers from the servers list. This method is thread safe.
        /// </summary>
        /// <param name="servers">A list of server contexts to remove.</param>
        public void RemoveServers(IEnumerable<ServerContext> servers)
        {
            if (servers == null)
                return;

            lock (_serversLock)
            {
                var serverList = new List<ServerContext>(Servers);

                // this algorithm compares server objects by _reference_, an that is intentional
                serverList.RemoveAll(servers.Contains);

                Servers = serverList.ToArray();
            }
        }
        /// <summary>
        /// Removes all servers from the servers list. This method is thread safe.
        /// </summary>
        internal void RemoveAllServers()
        {
            lock (_serversLock)
            {
                Servers = new ServerContext[0];
            }
        }

        /// <summary>
        /// Number of bytes sent to the server in one chunk during upload operations. Default: 10 MB.
        /// </summary>
        public int ChunkSizeInBytes { get; set; } = 10485760;
    }
}
