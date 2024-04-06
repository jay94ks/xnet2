using Xnet.Internals;
using Xnet.Packets.Impls;

namespace Xnet.Packets
{
    /// <summary>
    /// Packet handler attribute.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class PacketHandlerAttribute : Attribute, IPacketHandler
    {
        /// <summary>
        /// Specify a type to use this attribute as inline extender.
        /// </summary>
        public Type Type { get; set; }

        /// <inheritdoc/>
        public virtual Task HandleAsync(Connection Conn, IPacket Packet, Func<Task> Next)
        {
            if (Type is null || Type.IsAbstract)
                return Next.Invoke();

            if (Type.IsAssignableTo(typeof(IPacketHandler)))
            {
                return Conn.Services
                    .Create<IPacketHandler>(Type)
                    .HandleAsync(Conn, Packet, Next);
            }

            var PacketType = Packet.GetType();
            var GenericType = typeof(IPacketHandler<>).MakeGenericType(PacketType);
            if (Type.IsAssignableTo(GenericType))
            {
                return HandleThroughGenericAdapter(Conn, Packet, Next, PacketType);
            }

            return Next.Invoke();
        }

        /// <summary>
        /// Instantiate <see cref="Type"/> instance and, 
        /// adapt it to <see cref="IPacketHandler"/>, then invoke it.
        /// </summary>
        /// <param name="Conn"></param>
        /// <param name="Packet"></param>
        /// <param name="Next"></param>
        /// <param name="PacketType"></param>
        /// <returns></returns>
        private Task HandleThroughGenericAdapter(Connection Conn, IPacket Packet, Func<Task> Next, Type PacketType)
        {
            var Handler = Conn.Services.Create<IPacketHandler>(Type);
            var Ctor = typeof(GenericAdapter<>)
                .MakeGenericType(PacketType)
                .GetConstructors()
                .FirstOrDefault();

            Handler = Ctor.Invoke(new object[] { Handler }) as IPacketHandler;
            return Handler.HandleAsync(Conn, Packet, Next);
        }
    }
}
