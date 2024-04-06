using Xnet.Sockets;

namespace Xnet.Internals.Hosting
{
    /// <summary>
    /// Server context.
    /// </summary>
    public class ServerContext
    {
        /// <summary>
        /// Socket.
        /// </summary>
        public Socket Socket { get; set; }

        /// <summary>
        /// Stopping token.
        /// </summary>
        public CancellationToken Stopping { get; set; }
    }
}
