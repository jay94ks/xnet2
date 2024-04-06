namespace Xnet.Packets
{
    /// <summary>
    /// Base class for request packets.
    /// </summary>
    public abstract class RequestBase : IRequest
    {
        /// <inheritdoc/>
        public Guid RequestId { get; set; }

        /// <inheritdoc/>
        public abstract void Encode(BinaryWriter Writer);

        /// <inheritdoc/>
        public abstract void Decode(BinaryReader Reader);

        /// <inheritdoc/>
        public abstract Task<IResponse> HandleAsync(Connection Conn);
    }
}
