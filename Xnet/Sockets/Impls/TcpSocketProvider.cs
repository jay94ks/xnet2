using System.Net;
using System.Net.Sockets;
using DSocket = System.Net.Sockets.Socket;

namespace Xnet.Sockets.Impls
{
    /// <summary>
    /// Tcp socket provider.
    /// </summary>
    internal class TcpSocketProvider : SocketProvider
    {
        /// <inheritdoc/>
        public override string Name => "tcp";

        /// <inheritdoc/>
        protected override async Task<Socket> OnConnectAsync(IPEndPoint RemoteEndpoint, CancellationToken Token)
        {
            var Socket = new DSocket(RemoteEndpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                await Socket.ConnectAsync(RemoteEndpoint, Token);
                return new TcpSocket(Socket, false);
            }

            catch
            {
                try { Socket.Close(); } catch { }
                try { Socket.Dispose(); } catch { }
            }

            return null;
        }

        /// <inheritdoc/>
        protected override SocketServer OnCreateServer(IPEndPoint LocalEndpoint)
        {
            return new TcpSocketServer(LocalEndpoint);
        }
    }
}
