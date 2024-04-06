using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xnet.Packets
{
    /// <summary>
    /// Packet provider.
    /// </summary>
    public interface IPacketProvider
    {
        /// <summary>
        /// Try to get the packet id if mapped.
        /// </summary>
        /// <param name="Packet"></param>
        /// <returns></returns>
        bool TryGetPacketId(IPacket Packet, out Guid PacketId);

        /// <summary>
        /// Create a new <see cref="IPacket"/> instance
        /// </summary>
        /// <returns></returns>
        IPacket TryCreate(Guid PacketId);

    }
    
}
