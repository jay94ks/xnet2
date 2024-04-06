using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xnet.Packets
{
    /// <summary>
    /// Default implementation for packet provider interface.
    /// </summary>
    internal class PacketProvider : IPacketProvider
    {
        private readonly Dictionary<Type, Guid> m_Mappings;
        private readonly Dictionary<Guid, Func<IPacket>> m_Ctor;

        /// <summary>
        /// Initialize a new <see cref="PacketProvider"/> instance.
        /// </summary>
        internal PacketProvider(
            IReadOnlyDictionary<Type, Guid> Mappings,
            IReadOnlyDictionary<Guid, Func<IPacket>> Ctors)
        {
            m_Mappings = new Dictionary<Type, Guid>(Mappings);
            m_Ctor = new Dictionary<Guid, Func<IPacket>>(Ctors);
        }

        /// <inheritdoc/>
        public bool TryGetPacketId(IPacket Packet, out Guid PacketId)
        {
            if (Packet is null)
            {
                PacketId = Guid.Empty;
                return false;
            }

            return m_Mappings.TryGetValue(Packet.GetType(), out PacketId);
        }

        /// <inheritdoc/>
        public IPacket TryCreate(Guid PacketId)
        {
            if (m_Ctor.TryGetValue(PacketId, out var Ctor))
                return Ctor.Invoke();

            return null;
        }


    }
}
