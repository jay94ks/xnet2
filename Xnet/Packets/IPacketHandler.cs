using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xnet.Packets
{
    /// <summary>
    /// Packet handler.
    /// </summary>
    public interface IPacketHandler
    {
        /// <summary>
        /// Handle the packet asynchronously.
        /// </summary>
        /// <param name="Conn"></param>
        /// <param name="Packet"></param>
        /// <param name="Next"></param>
        /// <returns></returns>
        Task HandleAsync(Connection Conn, IPacket Packet, Func<Task> Next);
    }

    /// <summary>
    /// Packet handler (generic version).
    /// These will be registered with adapter.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IPacketHandler<T> where T : IPacket
    {
        /// <summary>
        /// Handle the packet asynchronously.
        /// </summary>
        /// <param name="Conn"></param>
        /// <param name="Packet"></param>
        /// <param name="Next"></param>
        /// <returns></returns>
        Task HandleAsync(Connection Conn, T Packet, Func<Task> Next);
    }
}
