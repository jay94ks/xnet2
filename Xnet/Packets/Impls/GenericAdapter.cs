using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xnet.Packets.Impls
{
    /// <summary>
    /// Generic adapter.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class GenericAdapter<T> : IPacketHandler, IPacketHandler<T> where T : IPacket
    {
        private readonly IPacketHandler<T> m_Handler;

        /// <summary>
        /// Initialize a new <see cref="GenericAdapter{T}"/> instance.
        /// </summary>
        /// <param name="Handler"></param>
        public GenericAdapter(IPacketHandler<T> Handler) => m_Handler = Handler;

        /// <summary>
        /// Create an adapter instance.
        /// </summary>
        /// <param name="Handler"></param>
        /// <returns></returns>
        public static IPacketHandler Create(IPacketHandler<T> Handler) => new GenericAdapter<T>(Handler);

        /// <inheritdoc/>
        public Task HandleAsync(Connection Conn, IPacket Packet, Func<Task> Next)
        {
            if (Packet is T Casted)
                return HandleAsync(Conn, Casted, Next);

            return Next.Invoke();
        }

        /// <inheritdoc/>
        public Task HandleAsync(Connection Conn, T Packet, Func<Task> Next)
            => m_Handler.HandleAsync(Conn, Packet, Next);
    }

}
