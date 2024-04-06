using System.Net;
using System.Net.Sockets;

namespace Xnet.Sockets.Impls
{
    /// <summary>
    /// Tcp based socket server.
    /// </summary>
    internal class TcpSocketServer : SocketServer
    {
        private readonly TcpListener m_Listener;

        /// <summary>
        /// Initialize a new <see cref="TcpSocketServer"/> instance.
        /// </summary>
        /// <param name="LocalEndpoint"></param>
        public TcpSocketServer(IPEndPoint LocalEndpoint)
        {
            (m_Listener = new TcpListener(LocalEndpoint)).Start();
        }

        /// <inheritdoc/>
        public override string Name => "tcp";

        /// <inheritdoc/>
        protected override void OnDisposing()
        {
            base.OnDisposing();
            try { m_Listener.Stop(); } catch { }
        }

        /// <inheritdoc/>
        protected override async Task<Socket> OnAcceptAsync(CancellationToken Token)
        {
            var Socket = await m_Listener.AcceptSocketAsync(Token);
            if (Socket is null)
                return null;

            return new TcpSocket(Socket, true);
        }
    }
}
