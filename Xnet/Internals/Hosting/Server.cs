using Microsoft.Extensions.Hosting;
using Xnet.Sockets;

namespace Xnet.Internals.Hosting
{
    /// <summary>
    /// Server service.
    /// </summary>
    internal class Server : BackgroundService
    {
        private readonly SocketServer m_Server;
        private readonly ILauncher m_Launcher;

        /// <summary>
        /// Initialize a new <see cref="Server"/> instance.
        /// </summary>
        public Server(ILauncher Accepter, SocketServer Server)
        {
            m_Server = Server;
            m_Launcher = Accepter;
        }

        /// <inheritdoc/>
        protected override async Task ExecuteAsync(CancellationToken Token)
        {
            await Helpers.EnsureAsync();
            using var __ = Token.Register(m_Server.Dispose, false);
            while (Token.IsCancellationRequested == false)
            {
                var Socket = await m_Server.AcceptAsync();
                if (Socket is null)
                    continue;

                var Context = new ServerContext
                {
                    Socket = Socket,
                    Stopping = Token
                };

                _ = RunAccepter(Context);
            }
        }

        /// <summary>
        /// Run the accepter.
        /// </summary>
        /// <param name="Context"></param>
        /// <returns></returns>
        private async Task RunAccepter(ServerContext Context)
        {
            await Helpers.EnsureAsync();
            await m_Launcher.LaunchAsync(Context);
        }
    }
}
