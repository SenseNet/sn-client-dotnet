using System;
using System.Collections.Generic;
using System.Linq;

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
        public static ClientContext Current { get; } = new ClientContext();

        [Obsolete("Initializing the client is not necessary anymore. Add servers using the Current singleton property instead.")]
        public static void Initialize(ServerContext[] servers)
        {
            if (servers != null)
                Current.Servers = servers;
        }

        //============================================================================= Instance API

        private readonly object _serversLock = new object();

        /// <summary>
        /// The available servers that can be a target of client operations.
        /// </summary>
        public ServerContext[] Servers { get; private set; } = new ServerContext[0];

        public ServerContext MainServer
        {
            get
            {
                if (Servers.Length < 1)
                    throw new InvalidOperationException("Please add at least one Server before using the client.");

                return Servers[0];
            }
        }
        /// <summary>
        /// One of the configured servers, chosen randomly.
        /// </summary>
        public ServerContext RandomServer
        {
            get
            {
                if (Servers.Length == 1)
                    return MainServer;
                return Servers[new Random().Next(0, Servers.Length)];
            }
        }

        public void AddServer(ServerContext server)
        {
            AddServers(new[] { server });
        }
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
        public void RemoveServer(ServerContext server)
        {
            RemoveServers(new []{ server });
        }
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
