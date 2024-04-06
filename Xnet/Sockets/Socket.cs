using System.Net;
using System.Net.Sockets;
using Xnet.Sockets.Impls;

namespace Xnet.Sockets
{
    /// <summary>
    /// Socket wrapper.
    /// To kick the connection, dispose it.
    /// </summary>
    public abstract class Socket : IDisposable
    {
        private static readonly SocketProvider[] PROVIDERS = new SocketProvider[]
        {
            new TcpSocketProvider()
        };

        // -- 
        private readonly CancellationTokenSource m_Closing;
        private int m_Disposed;

        /// <summary>
        /// Initialize a new <see cref="Socket"/> instance.
        /// </summary>
        public Socket()
        {
            Closing = (m_Closing = new()).Token;
        }

        /// <summary>
        /// Get the socket provider instance by its name.
        /// </summary>
        /// <param name="Name"></param>
        /// <returns></returns>
        public static SocketProvider Get(string Name)
        {
            Name = (Name ?? string.Empty);
            return PROVIDERS
                .Where(X => X.Name.Equals(Name, StringComparison.OrdinalIgnoreCase))
                .FirstOrDefault();
        }

        /// <summary>
        /// Tcp Socket Provider.
        /// </summary>
        public static SocketProvider Tcp => PROVIDERS[0];

        /// <summary>
        /// Name of the socket provider.
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// Indicates whether the socket is server mode or not.
        /// </summary>
        public abstract bool IsServerMode { get; }

        /// <summary>
        /// Triggered when the socket is closing.
        /// </summary>
        public CancellationToken Closing { get; }

        /// <summary>
        /// Returns the remote endpoint.
        /// </summary>
        public abstract IPEndPoint RemoteEndpoint { get; }

        /// <summary>
        /// Determines whether the error represents retry-able or not.
        /// </summary>
        /// <param name="Error"></param>
        /// <returns></returns>
        private static bool CanRetry(SocketException Error)
        {
            switch (Error.SocketErrorCode)
            {
                case SocketError.Interrupted:
                case SocketError.WouldBlock:
                case SocketError.IOPending:
                case SocketError.InProgress:
                case SocketError.AlreadyInProgress:
                    return true;

                default:
                    break;
            }
            return false;
        }

        /// <summary>
        /// Receive bytes from the remote host.
        /// </summary>
        /// <param name="Buffer"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        public async Task<int> ReceiveAsync(ArraySegment<byte> Buffer, CancellationToken Token = default)
        {
            using var Cts = CancellationTokenSource.CreateLinkedTokenSource(Closing, Token);
            if (Closing.IsCancellationRequested)
                return 0;

            while(true)
            {

                try { return await OnReceiveAsync(Buffer, Cts.Token); }
                catch (SocketException Error) when (CanRetry(Error)) { continue; }
                catch
                {
                    Token.ThrowIfCancellationRequested();
                    Dispose();
                }

                return 0;
            }
        }

        /// <summary>
        /// Send bytes to the remote host.
        /// </summary>
        /// <param name="Buffer"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        public async Task<int> SendAsync(ArraySegment<byte> Buffer, CancellationToken Token = default)
        {
            using var Cts = CancellationTokenSource.CreateLinkedTokenSource(Closing, Token);
            while (true)
            {
                try { return await OnSendAsync(Buffer, Cts.Token); }
                catch (SocketException Error) when (CanRetry(Error)) { continue; }
                catch
                {
                    Token.ThrowIfCancellationRequested();
                    Dispose();
                }

                return 0;
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (Interlocked.CompareExchange(ref m_Disposed, 1, 0) != 0)
                return;

            m_Closing.Cancel();
            m_Closing.Dispose();

            OnDisposing();
        }

        /// <summary>
        /// Called when the socket is disposing.
        /// </summary>
        protected virtual void OnDisposing()
        {
        }

        /// <summary>
        /// Called to receive bytes from the remote host.
        /// Note that <paramref name="Token"/> is a token that combined closing token and user specified token.
        /// </summary>
        /// <param name="Buffer"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        protected abstract Task<int> OnReceiveAsync(ArraySegment<byte> Buffer, CancellationToken Token);

        /// <summary>
        /// Called to send bytes to the remote host.
        /// Note that <paramref name="Token"/> is a token that combined closing token and user specified token.
        /// </summary>
        /// <param name="Buffer"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        protected abstract Task<int> OnSendAsync(ArraySegment<byte> Buffer, CancellationToken Token);
    }
}
