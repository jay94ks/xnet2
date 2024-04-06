using Xnet.Sockets;

namespace Xnet.Internals.Hosting
{
    /// <summary>
    /// Client context.
    /// </summary>
    public class ClientContext
    {
        /// <summary>
        /// Socket.
        /// </summary>
        public Socket Socket { get; set; }

        /// <summary>
        /// Callback.
        /// </summary>
        public Action<Connection> Callback { get; set; }

        /// <summary>
        /// Stopping token.
        /// </summary>
        public CancellationToken Stopping { get; set; }
    }
}
