using Microsoft.Extensions.Hosting;
using System.Diagnostics;
using System.Net;
using Xnet.Sockets;

namespace Xnet.Internals.Hosting
{
    /// <summary>
    /// Client service.
    /// </summary>
    internal class Client : BackgroundService
    {
        private readonly Queue<ClientRequest> m_Requests;
        private readonly ILauncher m_Launcher;
        private readonly ClientQueue m_Queue;

        /// <summary>
        /// Initialize a new <see cref="Client"/> instance.
        /// </summary>
        public Client(
            ClientQueue Queue,
            ILauncher Launcher,
            IEnumerable<ClientRequest> Requests)
        {
            m_Queue = Queue;
            m_Launcher = Launcher;
            m_Requests = new Queue<ClientRequest>(Requests);
        }

        /// <inheritdoc/>
        protected override async Task ExecuteAsync(CancellationToken Token)
        {
            await Helpers.EnsureAsync();
            using var __ = Token.Register(m_Queue.Dispose, false);
            while (Token.IsCancellationRequested == false)
            {
                if (m_Requests.TryDequeue(out var Request) == false)
                {
                    if ((Request = await m_Queue.DequeueAsync(Token)) is null)
                        continue;
                }

                if (Request is null)
                    continue;

                if (Request.SocketProvider is null)
                {
                    // --> report failure.
                    SafeInvoke(() => Request.Callback?.Invoke(null));
                    continue;
                }

                var Socket = await ConnectAsync(Request, Token);
                if (Socket is null)
                {
                    // --> report failure.
                    SafeInvoke(() => Request.Callback?.Invoke(null));
                    continue;
                }

                var Context = new ClientContext
                {
                    Socket = Socket,
                    Callback = Request.Callback,
                    Stopping = Token
                };

                _ = RunAccepter(Context);
            }
        }

        /// <summary>
        /// Run the launcher.
        /// </summary>
        /// <param name="Context"></param>
        /// <returns></returns>
        private async Task RunAccepter(ClientContext Context)
        {
            await Helpers.EnsureAsync();
            await m_Launcher.LaunchAsync(Context);
        }

        /// <summary>
        /// Connect to the remote host asynchronously.
        /// </summary>
        /// <param name="Request"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        private async Task<Socket> ConnectAsync(ClientRequest Request, CancellationToken Token)
        {
            using var Cts = CancellationTokenSource.CreateLinkedTokenSource(Token);
            Cts.CancelAfter(TimeSpan.FromSeconds(30));

            var SocketProvider = Request.SocketProvider;
            if (Request.RemoteAddress is null)
            {
                if (string.IsNullOrWhiteSpace(Request.RemoteHost))
                    return null;

                return await SocketProvider.ConnectAsync(
                    Request.RemoteHost, Request.RemotePort, Cts.Token);
            }

            return await SocketProvider.ConnectAsync(
                    new IPEndPoint(Request.RemoteAddress, Request.RemotePort),
                    Cts.Token);
        }

        /// <summary>
        /// Safe invoke.
        /// </summary>
        /// <param name="Any"></param>
        private void SafeInvoke(Action Any)
        {
            if (Debugger.IsAttached)
            {
                Any?.Invoke();
                return;
            }

            try { Any?.Invoke(); }
            catch
            {
            }
        }
    }
}
