namespace Xnet.Packets.Impls
{
    internal class DelegatePacketHandler<TPacket> : IPacketHandler<TPacket> where TPacket : IPacket
    {
        private readonly Func<Connection, TPacket, Func<Task>, Task> m_Handler;

        /// <summary>
        /// Initialize a new <see cref="DelegatePacketHandler{TPacket}"/> instance.
        /// </summary>
        /// <param name="Greeter"></param>
        public DelegatePacketHandler(Func<Connection, TPacket, Func<Task>, Task> Greeter)
            => m_Handler = Greeter;

        /// <inheritdoc/>
        public Task HandleAsync(Connection Conn, TPacket Packet, Func<Task> Next)
        {
            return m_Handler.Invoke(Conn, Packet, Next);
        }
    }

}
