using System.Net;
using Xnet.Sockets;

namespace Xnet.Internals.Hosting
{
    /// <summary>
    /// Client request.
    /// </summary>
    internal class ClientRequest
    {
        /// <summary>
        /// Remote host. this refered if <see cref="RemoteAddress"/> is not specified.
        /// </summary>
        public string RemoteHost { get; set; }

        /// <summary>
        /// Remote address.
        /// </summary>
        public IPAddress RemoteAddress { get; set; }

        /// <summary>
        /// Port.
        /// </summary>
        public int RemotePort { get; set; }

        /// <summary>
        /// Socket provider.
        /// </summary>
        public SocketProvider SocketProvider { get; set; }

        /// <summary>
        /// Callback.
        /// </summary>
        public Action<Connection> Callback { get; set; }
    }
}
