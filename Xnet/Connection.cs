using Microsoft.Extensions.DependencyInjection;
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
        private readonly ConnectionAccessor m_Accessor;

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

            m_Accessor = Services.GetRequiredService<ConnectionAccessor>();
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

        /// <summary>
        /// Emit a packet to the remote host asynchronously.
        /// </summary>
        /// <param name="Packet"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        public async Task<bool> EmitAsync(IPacket Packet, CancellationToken Token = default)
        {
            // --> add null dispatcher to avoid duplication of request id.
            if (Packet is IRequest Request)
            {
                Request.RequestId = Guid.NewGuid();

                while (m_Pendings.TryAdd(Request.RequestId, null) == false)
                    Request.RequestId = Guid.NewGuid();
            }

            return await EmitInternalAsync(Packet, Token);
        }

        /// <summary>
        /// Execute a request on the remote host and wait its response.
        /// </summary>
        /// <param name="Request"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        public async Task<IResponse> ExecuteAsync(IRequest Request, CancellationToken Token = default)
        {
            var Rid = Request.RequestId = Guid.NewGuid();

            var Tcs = new TaskCompletionSource<IResponse>();
            while (m_Pendings.TryAdd(Request.RequestId, Tcs) == false)
                Request.RequestId = Rid = Guid.NewGuid();

            // --> replace tcs to null on pending dispatchers.
            void ReplaceToNullDispatcher()
            {
                m_Pendings.TryUpdate(Rid, null, Tcs);

                if (Tcs.Task.IsCompleted)
                    m_Pendings.TryRemove(Rid, out _);

                else
                    Tcs?.TrySetResult(null);
            }

            try
            {
                if (await EmitInternalAsync(Request, Token) == false)
                    return null;

                using (Token.Register(ReplaceToNullDispatcher, false))
                    return await Tcs.Task.ConfigureAwait(false);
            }

            finally
            {
                if (m_Pendings.TryGetValue(Rid, out var Temp) && Temp == Tcs)
                    m_Pendings.TryRemove(Rid, out _);

                Tcs?.TrySetResult(null);
            }
        }

        /// <summary>
        /// Emit a packet to the remote host asynchronously.
        /// </summary>
        /// <param name="Packet"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        private async Task<bool> EmitInternalAsync(IPacket Packet, CancellationToken Token = default)
        {
            var Bytes = Encode(Packet);
            if (Bytes is null)
                return false;

            try { await m_EmitLock.WaitAsync(Token); }
            catch
            {
                return false;
            }
            try
            {
                var Segment = new ArraySegment<byte>(Bytes);
                while (Segment.Count > 0)
                {
                    var Length = await m_Socket.SendAsync(Segment);
                    if (Length <= 0)
                        break;

                    Segment = Segment.Slice(Length);
                }

                return Segment.Count <= 0;
            }

            finally
            {
                try { m_EmitLock.Release(); }
                catch
                {
                }
            }
        }
    }
}