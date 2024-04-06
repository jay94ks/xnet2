namespace Xnet.Sockets
{
    /// <summary>
    /// Socket Server.
    /// </summary>
    public abstract class SocketServer : IDisposable
    {
        private readonly CancellationTokenSource m_Disposing;
        private int m_Disposed;

        /// <summary>
        /// Initialize a new <see cref="SocketServer"/> instance.
        /// </summary>
        public SocketServer()
        {
            Disposing = (m_Disposing = new()).Token;
        }

        /// <summary>
        /// Name of the socket provider.
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// Triggered when the socket server is disposing.
        /// </summary>
        public CancellationToken Disposing { get; }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (Interlocked.CompareExchange(ref m_Disposed, 1, 0) != 0)
                return;

            m_Disposing.Cancel();
            m_Disposing.Dispose();

            OnDisposing();
        }

        /// <summary>
        /// Called when the socket is disposing.
        /// </summary>
        protected virtual void OnDisposing()
        {
        }

        /// <summary>
        /// Accept a new connection.
        /// This returns null if failed.
        /// </summary>
        /// <param name="Token"></param>
        /// <returns></returns>
        public async Task<Socket> AcceptAsync(CancellationToken Token = default)
        {
            using var Cts = CancellationTokenSource.CreateLinkedTokenSource(Disposing, Token);
            while (Disposing.IsCancellationRequested == false)
            {
                Socket Socket;

                try { Socket = await OnAcceptAsync(Cts.Token); }
                catch
                {
                    Token.ThrowIfCancellationRequested();
                    continue;
                }

                return Socket;
            }

            return null;
        }

        /// <summary>
        /// Called to accept new connection.
        /// Note that the token is same with <see cref="Disposing"/> token.
        /// </summary>
        /// <param name="Token"></param>
        /// <returns></returns>
        protected abstract Task<Socket> OnAcceptAsync(CancellationToken Token);

    }
}
