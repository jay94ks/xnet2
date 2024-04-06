using System.Net;
using Xnet.Internals;
using Xnet.Packets;
using Xnet.Sockets;

namespace Xnet
{
    /// <summary>
    /// A connection of Xnet.
    /// </summary>
    public sealed partial class Connection
    {
        private readonly Socket m_Socket;
        private readonly SemaphoreSlim m_EmitLock = new(1);
        private readonly IPacketProvider[] m_Providers;
        private readonly IPacketHandler[] m_Handlers;

        /// <summary>
        /// Initialize a new <see cref="Connection"/> instance.
        /// </summary>
        /// <param name="Socket"></param>
        internal Connection(IServiceProvider Services, Socket Socket)
        {
            this.Services = Services;

            m_Providers = Services
                .GetEnumerableServices<IPacketProvider>()
                .ToArray();

            m_Handlers = Services
                .GetEnumerableServices<IPacketHandler>()
                .ToArray();

            (m_Socket = Socket).Closing.Register(OnClosing, false);
        }

        /// <summary>
        /// Triggered when the connection is closing.
        /// </summary>
        public CancellationToken Closing => m_Socket.Closing;

        /// <summary>
        /// Remote endpoint.
        /// </summary>
        public IPEndPoint RemoteEndpoint => m_Socket.RemoteEndpoint;

        /// <summary>
        /// Service provider, scoped.
        /// </summary>
        public IServiceProvider Services { get; }

        /// <summary>
        /// Kick the connection.
        /// </summary>
        public void Kick() => m_Socket.Dispose();

        /// <summary>
        /// Called when the connection is closing.
        /// </summary>
        private void OnClosing()
        {
            try { m_EmitLock.Dispose(); }
            catch
            {
            }
        }
    }
}