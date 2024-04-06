using System.Reflection;

namespace Xnet.Packets
{
    /// <summary>
    /// Packet attribute,
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false)]
    public class PacketAttribute : Attribute
    {
        /// <summary>
        /// Specifies name of the packet.
        /// This will be used to generate packet id.
        /// If nothing specified, the class's full name will be used.
        /// </summary>
        public string Name { get; set; }
    }
}
