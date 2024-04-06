using Xnet.Internals.Hosting;

namespace Xnet.Internals
{
    /// <summary>
    /// Client side socket accepter.
    /// </summary>
    public interface ILauncher
    {
        /// <summary>
        /// Called when the new socket connected by client service.
        /// </summary>
        /// <param name="Context"></param>
        /// <returns></returns>
        Task LaunchAsync(ClientContext Context);

        /// <summary>
        /// Called when the new socket connected from the remote host.
        /// </summary>
        /// <param name="Context"></param>
        /// <returns></returns>
        Task LaunchAsync(ServerContext Context);
    }
}
