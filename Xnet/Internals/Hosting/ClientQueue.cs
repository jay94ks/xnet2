using System.Threading.Channels;

namespace Xnet.Internals.Hosting
{
    /// <summary>
    /// Client request queue.
    /// </summary>
    internal class ClientQueue : IDisposable
    {
        private readonly Channel<ClientRequest> m_Queue;

        /// <summary>
        /// Initialize a new <see cref="ClientQueue"/> instance.
        /// </summary>
        public ClientQueue()
        {
            m_Queue = Channel.CreateBounded<ClientRequest>(32);
        }

        /// <summary>
        /// Enqueue a request object to the queue.
        /// </summary>
        /// <param name="Request"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        public async Task<bool> EnqueueAsync(ClientRequest Request, CancellationToken Token = default)
        {
            try
            {
                await m_Queue.Writer.WriteAsync(Request, Token);
                return true;
            }

            catch { }
            return false;
        }

        /// <summary>
        /// Dequeue a request object from the queue.
        /// </summary>
        /// <param name="Token"></param>
        /// <returns></returns>
        public async Task<ClientRequest> DequeueAsync(CancellationToken Token = default)
        {
            try { return await m_Queue.Reader.ReadAsync(Token); }
            catch
            {
            }

            return null;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            try { m_Queue.Writer.Complete(); }
            catch
            {
            }
        }
    }
}
