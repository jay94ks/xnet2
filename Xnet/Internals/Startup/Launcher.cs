using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xnet.Internals.Hosting;
using Xnet.Packets;

namespace Xnet.Internals.Startup
{
    /// <summary>
    /// Client launcher.
    /// </summary>
    internal class Launcher : ILauncher
    {
        private readonly IServiceProvider m_Services;

        /// <summary>
        /// Initialize a new <see cref="Launcher"/> instance.
        /// </summary>
        /// <param name="Services"></param>
        public Launcher(IServiceProvider Services)
        {
            m_Services = Services;
        }

        /// <inheritdoc/>
        public async Task LaunchAsync(ClientContext Context)
        {
            using var Scope = m_Services.CreateScope();
            var Greeters = Scope.ServiceProvider
                .GetEnumerableServices<IGreeter>();

            var Conn = new Connection(Scope.ServiceProvider, Context.Socket);

            // --> run the connection loop.
            try
            {
                await Greeters.Pipeline(
                    (Pipe, Next) => Pipe.GreetingAsync(Conn, Next),
                    () =>
                    {
                        Context.Callback?.Invoke(Conn);
                        return RunLoop(Conn, Context.Stopping);
                    });
            }

            finally
            {
                Conn.Kick();
            }
        }

        /// <inheritdoc/>
        public async Task LaunchAsync(ServerContext Context)
        {
            using var Scope = m_Services.CreateScope();
            var Greeters = Scope.ServiceProvider
                .GetEnumerableServices<IGreeter>();

            var Conn = new Connection(Scope.ServiceProvider, Context.Socket);

            // --> run the connection loop.
            try
            {
                await Greeters.Pipeline(
                    (Pipe, Next) => Pipe.GreetingAsync(Conn, Next),
                    () => RunLoop(Conn, Context.Stopping));
            }

            finally
            {
                Conn.Kick();
            }
        }

        /// <summary>
        /// Run the connection loop.
        /// </summary>
        /// <param name="Conn"></param>
        /// <param name="Stopping"></param>
        /// <returns></returns>
        private async Task RunLoop(Connection Conn, CancellationToken Stopping)
        {
            // --> run the connection loop.
            using (Stopping.Register(() => Conn.Kick()))
                await Conn.RunLoop();
        }
    }
}