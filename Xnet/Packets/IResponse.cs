namespace Xnet.Packets
{
    /// <summary>
    /// Response interface.
    /// </summary>
    public interface IResponse : IPacket
    {
        /// <summary>
        /// Request ID to trace requests.
        /// This will be written by framework.
        /// </summary>
        Guid RequestId { get; set; }
    }
}
