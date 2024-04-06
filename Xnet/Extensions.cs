using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using Xnet.Internals;
using Xnet.Internals.Hosting;
using Xnet.Internals.Protocols;
using Xnet.Internals.Startup;
using Xnet.Packets;
using Xnet.Packets.Impls;
using Xnet.Sockets;

namespace Xnet
{
    /// <summary>
    /// Extensions.
    /// </summary>
    public static class Extensions
    {
        private class Dummy
        {

        }

        private class NetworkBuilder : INetworkBuilder
        {
            /// <summary>
            /// Service collection.
            /// </summary>
            public IServiceCollection Services { get; set; }
        }

        /// <summary>
        /// Add the Xnet.
        /// </summary>
        /// <param name="Services"></param>
        /// <returns></returns>
        public static IServiceCollection AddXnet(this IServiceCollection Services, Action<INetworkBuilder> Configure = null)
        {
            var Nb = new NetworkBuilder
            {
                Services = Services
            };

            Configure?.Invoke(Nb);

            if (Services.Any(X => X.ServiceType == typeof(Dummy)))
                return Services;


            Nb
                .UseGreeter<PingPong.Greeter>()
                .UseHandler<PingPong.Hooker>()
                .UseMapper(Mapper =>
                {
                    Mapper
                        .Map<PingPong.Ping>()
                        .Map<PingPong.Pong>();
                });

            // --> redirect to connection accessor.
            Services.AddTransient(Services =>
            {
                return Services
                    .GetRequiredService<ConnectionAccessor>()
                    .Current;
            });

            return Services
                .AddSingleton<Dummy>()
                .AddSingleton<ClientQueue>()
                .AddSingleton<ConnectionAccessor>()
                .AddSingleton<ILauncher, Launcher>()
                .AddSingleton<PingPong.Manager>()
                .AddScoped<PingPong.State>()
                .AddHostedService<Client>()
                .AddHostedService<PingPong.Service>();
        }

        /// <summary>
        /// Listen <paramref name="LocalEndpoint"/>.
        /// </summary>
        /// <param name="Network"></param>
        /// <param name="LocalEndpoint"></param>
        /// <param name="SocketProvider"></param>
        /// <returns></returns>
        public static INetworkBuilder Listen(this INetworkBuilder Network, IPEndPoint LocalEndpoint, SocketProvider SocketProvider = null)
        {
            if (SocketProvider is null)
                SocketProvider = Socket.Tcp;

            Network.Services.AddHostedService(Services =>
            {
                var Launcher = Services.GetRequiredService<ILauncher>();
                return new Server(Launcher, SocketProvider.CreateServer(LocalEndpoint));
            });

            return Network;
        }

        /// <summary>
        /// Connect to the remote peer.
        /// </summary>
        /// <param name="Network"></param>
        /// <param name="RemoteHost"></param>
        /// <param name="RemotePort"></param>
        /// <param name="Callback"></param>
        /// <param name="SocketProvider"></param>
        /// <returns></returns>
        public static INetworkBuilder Connect(this INetworkBuilder Network, string RemoteHost, int RemotePort, Action<Connection> Callback = null, SocketProvider SocketProvider = null)
        {
            Network.Services.AddSingleton(new ClientRequest
            {
                RemoteHost = RemoteHost,
                RemotePort = RemotePort,
                Callback = Callback,
                SocketProvider = SocketProvider ?? Socket.Tcp
            });

            return Network;
        }
        /// <summary>
        /// Connect to the remote peer.
        /// </summary>
        /// <param name="Network"></param>
        /// <param name="RemoteEndpoint"></param>
        /// <param name="Callback"></param>
        /// <param name="SocketProvider"></param>
        /// <returns></returns>
        public static INetworkBuilder Connect(this INetworkBuilder Network, IPEndPoint RemoteEndpoint, Action<Connection> Callback = null, SocketProvider SocketProvider = null)
        {
            Network.Services.AddSingleton(new ClientRequest
            {
                RemoteAddress = RemoteEndpoint.Address,
                RemotePort = RemoteEndpoint.Port,
                Callback = Callback,
                SocketProvider = SocketProvider ?? Socket.Tcp
            });

            return Network;
        }

        /// <summary>
        /// Add the packet provider.
        /// </summary>
        /// <typeparam name="TProvider"></typeparam>
        /// <param name="Network"></param>
        /// <returns></returns>
        public static INetworkBuilder UseProvider<TProvider>(this INetworkBuilder Network) where TProvider : class, IPacketProvider
        {
            Network.Services.AddSingleton<IPacketProvider, TProvider>();
            return Network;
        }

        /// <summary>
        /// Add the packet provider.
        /// </summary>
        /// <typeparam name="TProvider"></typeparam>
        /// <param name="Network"></param>
        /// <returns></returns>
        public static INetworkBuilder UseProvider<TProvider>(this INetworkBuilder Network, TProvider Provider) where TProvider : class, IPacketProvider
        {
            Network.Services.AddSingleton<IPacketProvider>(Provider);
            return Network;
        }

        /// <summary>
        /// Add the packet handler.
        /// </summary>
        /// <typeparam name="THandler"></typeparam>
        /// <param name="Network"></param>
        /// <returns></returns>
        public static INetworkBuilder UseHandler<THandler>(this INetworkBuilder Network) where THandler : class, IPacketHandler
        {
            Network.Services.AddSingleton<IPacketHandler, THandler>();
            return Network;
        }

        /// <summary>
        /// Add the packet handler.
        /// </summary>
        /// <typeparam name="THandler"></typeparam>
        /// <param name="Network"></param>
        /// <returns></returns>
        public static INetworkBuilder UseHandler<THandler>(this INetworkBuilder Network, THandler Handler) where THandler : class, IPacketHandler
        {
            Network.Services.AddSingleton<IPacketHandler>(Handler);
            return Network;
        }

        /// <summary>
        /// Add the packet handler.
        /// </summary>
        /// <typeparam name="TPacket"></typeparam>
        /// <param name="Network"></param>
        /// <returns></returns>
        public static INetworkBuilder UseHandler<TPacket, THandler>(this INetworkBuilder Network) where TPacket : IPacket where THandler : class, IPacketHandler<TPacket>
        {
            Network.Services
                .AddTransient<THandler>()
                .AddSingleton(Services =>
                {
                    return GenericAdapter<TPacket>.Create(
                        Services.GetRequiredService<THandler>());
                });

            return Network;
        }

        /// <summary>
        /// Add the packet handler.
        /// </summary>
        /// <typeparam name="TPacket"></typeparam>
        /// <param name="Network"></param>
        /// <returns></returns>
        public static INetworkBuilder UseHandler<TPacket, THandler>(this INetworkBuilder Network, THandler Handler) where TPacket : IPacket where THandler : class, IPacketHandler<TPacket>
        {
            Network.Services
                .AddSingleton(Services =>
                {
                    return GenericAdapter<TPacket>.Create(Handler);
                });

            return Network;
        }

        /// <summary>
        /// Add the packet handler.
        /// </summary>
        /// <typeparam name="TPacket"></typeparam>
        /// <param name="Network"></param>
        /// <param name="Delegate"></param>
        /// <returns></returns>
        public static INetworkBuilder UseHandler<TPacket>(this INetworkBuilder Network, Func<Connection, TPacket, Func<Task>, Task> Delegate) where TPacket : IPacket
        {
            Network.Services
                .AddSingleton(Services =>
                {
                    return GenericAdapter<TPacket>.Create(
                        new DelegatePacketHandler<TPacket>(Delegate));
                });

            return Network;
        }

        /// <summary>
        /// Enable the greeter for all connections.
        /// </summary>
        /// <typeparam name="TGreeter"></typeparam>
        /// <param name="Network"></param>
        /// <returns></returns>
        public static INetworkBuilder UseGreeter<TGreeter>(this INetworkBuilder Network) where TGreeter : class, IGreeter
        {
            Network.Services.AddSingleton<IGreeter, TGreeter>();
            return Network;
        }

        /// <summary>
        /// Enable the greeter for all connections.
        /// </summary>
        /// <typeparam name="TGreeter"></typeparam>
        /// <param name="Network"></param>
        /// <returns></returns>
        public static INetworkBuilder UseGreeter<TGreeter>(this INetworkBuilder Network, TGreeter Greeter) where TGreeter : class, IGreeter
        {
            Network.Services.AddSingleton<IGreeter>(Greeter);
            return Network;
        }

        /// <summary>
        /// Enable the greeter for all connections by delegate.
        /// </summary>
        /// <param name="Network"></param>
        /// <param name="Delegate"></param>
        /// <returns></returns>
        public static INetworkBuilder UseGreeter(this INetworkBuilder Network, Func<Connection, Func<Task>, Task> Delegate)
        {
            return UseGreeter(Network, new DelegateGreeter(Delegate));
        }

        /// <summary>
        /// Add packet providers that created by packet provider builder.
        /// </summary>
        /// <param name="Network"></param>
        /// <param name="Mapper"></param>
        /// <returns></returns>
        public static INetworkBuilder UseMapper(this INetworkBuilder Network, Action<PacketProviderBuilder> Mapper)
        {
            if (Mapper != null)
            {
                var Ppb = new PacketProviderBuilder();
                Network.Services.AddSingleton(_ => Ppb.Build());
                Mapper.Invoke(Ppb);
            }

            return Network;
        }
    }
}
