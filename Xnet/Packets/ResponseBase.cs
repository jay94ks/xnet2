namespace Xnet.Packets
{
    /// <summary>
    /// Base class for response packets.
    /// </summary>
    public abstract class ResponseBase : IResponse
    {
        /// <inheritdoc/>
        public Guid RequestId { get; set; }

        /// <inheritdoc/>
        public abstract void Encode(BinaryWriter Writer);

        /// <inheritdoc/>
        public abstract void Decode(BinaryReader Reader);

    }
}
