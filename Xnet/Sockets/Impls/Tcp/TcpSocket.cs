using System.Net;
using System.Net.Sockets;
using DSocket = System.Net.Sockets.Socket;

namespace Xnet.Sockets.Impls
{
    internal class TcpSocket : Socket
    {
        private readonly DSocket m_Socket;

        /// <summary>
        /// Initialize a new <see cref="TcpSocket"/> instance.
        /// </summary>
        /// <param name="Socket"></param>
        public TcpSocket(DSocket Socket, bool ServerMode)
        {
            m_Socket = Socket;
            try
            {
                Socket.NoDelay = true;
                Socket.Blocking = false;
            }
            catch { }

            IsServerMode = ServerMode;
            RemoteEndpoint = Socket.RemoteEndPoint as IPEndPoint;
        }

        /// <inheritdoc/>
        public override string Name => "tcp";

        /// <inheritdoc/>
        public override bool IsServerMode { get; }

        /// <inheritdoc/>
        public override IPEndPoint RemoteEndpoint { get; }

        /// <inheritdoc/>
        protected override void OnDisposing()
        {
            base.OnDisposing();
            try { m_Socket.Close(); } catch { }
            try { m_Socket.Dispose(); } catch { }
        }

        /// <inheritdoc/>
        protected override async Task<int> OnReceiveAsync(ArraySegment<byte> Buffer, CancellationToken Token)
        {
            return await m_Socket.ReceiveAsync(Buffer, SocketFlags.None, Token);
        }

        /// <inheritdoc/>
        protected override async Task<int> OnSendAsync(ArraySegment<byte> Buffer, CancellationToken Token)
        {
            return await m_Socket.SendAsync(Buffer, SocketFlags.None, Token);
        }
    }
}
