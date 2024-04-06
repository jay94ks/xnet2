namespace Xnet.Packets
{
    /// <summary>
    /// Request interface.
    /// </summary>
    public interface IRequest : IPacket
    {
        /// <summary>
        /// Request ID to trace requests.
        /// This will be written by framework.
        /// </summary>
        Guid RequestId { get; set; }

        /// <summary>
        /// Handle the request packet.
        /// </summary>
        /// <param name="Conn"></param>
        /// <returns></returns>
        Task<IResponse> HandleAsync(Connection Conn);
    }
}
