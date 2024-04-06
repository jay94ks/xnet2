namespace Xnet.Packets
{
    /// <summary>
    /// Packet interface.
    /// </summary>
    public interface IPacket
    {
        /// <summary>
        /// Encode the packet payload to the writer,
        /// </summary>
        /// <param name="Writer"></param>
        void Encode(BinaryWriter Writer);

        /// <summary>
        /// Decode the packet payload from the reader.
        /// </summary>
        /// <param name="Reader"></param>
        void Decode(BinaryReader Reader);
    }
}
