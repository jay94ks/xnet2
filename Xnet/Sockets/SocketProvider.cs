using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Xnet.Sockets
{
    /// <summary>
    /// Socket provider.
    /// </summary>
    public abstract class SocketProvider
    {
        /// <summary>
        /// Name of the socket provider.
        /// e.g. tcp, rudp, ...
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// Returns whether the system supports quic transport.
        /// </summary>
        public virtual bool IsSupported => true;
        
        /// <summary>
        /// Connect to the remote host.
        /// This connects to random host address that resolved from name server.
        /// </summary>
        /// <param name="Host"></param>
        /// <param name="Port"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        public async Task<Socket> ConnectAsync(string Host, int Port, CancellationToken Token = default)
        {
            try
            {
                if (IPAddress.TryParse(Host, out var Address))
                    return await ConnectAsync(new IPEndPoint(Address, Port), Token);

                var Addrs = await Dns.GetHostAddressesAsync(Host);
                foreach(var EachAddr in Addrs.OrderBy(_ => Guid.NewGuid()))
                {
                    using var Cts = CancellationTokenSource.CreateLinkedTokenSource(Token);
                    Cts.CancelAfter(TimeSpan.FromSeconds(30));

                    var Socket = await ConnectAsync(new IPEndPoint(EachAddr, Port), Cts.Token);
                    if (Socket is null)
                        continue;

                    return Socket;
                }
            }
            catch { }
            return null;
        }

        /// <summary>
        /// Connect to the remote host.
        /// If failed to connect to the remote endpoint, this returns null.
        /// </summary>
        /// <param name="RemoteEndpoint"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        public async Task<Socket> ConnectAsync(IPEndPoint RemoteEndpoint, CancellationToken Token = default)
        {
            Socket Socket = null;
            try { Socket = await OnConnectAsync(RemoteEndpoint, Token); }
            catch
            {
            }

            return Socket;
        }

        /// <summary>
        /// Create a new <see cref="SocketServer"/> instance.
        /// If failed to listen the specified endpoint, this returns null.
        /// </summary>
        /// <param name="LocalEndpoint"></param>
        /// <returns></returns>
        public SocketServer CreateServer(IPEndPoint LocalEndpoint)
        {
            try { return OnCreateServer(LocalEndpoint); }
            catch
            {
            }

            return null;
        }

        /// <summary>
        /// Called to connect to the remote host asynchronously.
        /// </summary>
        /// <param name="RemoteEndpoint"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        protected abstract Task<Socket> OnConnectAsync(IPEndPoint RemoteEndpoint, CancellationToken Token);

        /// <summary>
        /// Called to create a server instance.
        /// </summary>
        /// <param name="LocalEndpoint"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        protected abstract SocketServer OnCreateServer(IPEndPoint LocalEndpoint);
    }
}
