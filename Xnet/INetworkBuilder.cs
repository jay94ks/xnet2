using Microsoft.Extensions.DependencyInjection;

namespace Xnet
{
    /// <summary>
    /// Network builder interface.
    /// </summary>
    public interface INetworkBuilder
    {
        /// <summary>
        /// Service collection.
        /// </summary>
        IServiceCollection Services { get; }
    }
}
