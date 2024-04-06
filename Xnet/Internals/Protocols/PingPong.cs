using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xnet.Packets;

namespace Xnet.Internals.Protocols
{
    /// <summary>
    /// Ping-Pong protocol.
    /// </summary>
    internal class PingPong
    {
        /// <summary>
        /// Ping-Pong State.
        /// </summary>
        public class State
        {
            /// <summary>
            /// Last RX time.
            /// </summary>
            public DateTime LastRx { get; set; }

            /// <summary>
            /// Last ACT time.
            /// </summary>
            public DateTime LastAct { get; set; }

            /// <summary>
            /// Counter.
            /// </summary>
            public int Counter { get; set; }

            /// <summary>
            /// Connection.
            /// </summary>
            public Connection Connection { get; set; }
        }

        /// <summary>
        /// State Manager.
        /// </summary>
        public class Manager : HashSet<State>
        {
        }

        /// <summary>
        /// Ping/Pong Service.
        /// </summary>
        public class Service : BackgroundService
        {
            private readonly Manager m_Manager;

            /// <summary>
            /// Initialize a new <see cref="Service"/> instance.
            /// </summary>
            /// <param name="Manager"></param>
            public Service(Manager Manager)
            {
                m_Manager = Manager;
            }

            /// <inheritdoc/>
            protected override async Task ExecuteAsync(CancellationToken Token)
            {
                await Helpers.EnsureAsync();
                while(Token.IsCancellationRequested == false)
                {
                    Queue<State> Queue;

                    try { await Task.Delay(TimeSpan.FromSeconds(1), Token); }
                    catch
                    {
                    }

                    lock (m_Manager)
                        Queue = new Queue<State>(m_Manager);

                    while (Queue.TryDequeue(out var State))
                    {
                        var Elapsed = DateTime.Now - State.LastRx;
                        if (Elapsed.TotalSeconds < 5)
                        {
                            State.LastAct = DateTime.Now;
                            State.Counter = 0;
                            continue;
                        }

                        Elapsed = DateTime.Now - State.LastAct;
                        if (Elapsed.TotalSeconds < 5)
                            continue;

                        State.LastAct = DateTime.Now;
                        if (State.Counter > 5)
                        {
                            State.Connection.Kick();
                            continue;
                        }

                        State.Counter++;
                        await State.Connection.EmitAsync(new Ping());
                    }
                }
            }
        }

        /// <summary>
        /// Greeter.
        /// </summary>
        public class Greeter : IGreeter
        {
            /// <inheritdoc/>
            public async Task GreetingAsync(Connection Conn, Func<Task> Next)
            {
                var Manager = Conn.Services.GetRequiredService<Manager>();
                var State = Conn.Services.GetRequiredService<State>();

                State.Connection = Conn;
                State.LastRx = DateTime.Now;

                lock (Manager)
                {
                    Manager.Add(State);
                }

                try { await Next.Invoke(); }
                finally
                {
                    lock (Manager)
                    {
                        Manager.Remove(State);
                    }
                }
            }
        }

        /// <summary>
        /// Packet hooker.
        /// </summary>
        public class Hooker : IPacketHandler
        {
            /// <inheritdoc/>
            public Task HandleAsync(Connection Conn, IPacket Packet, Func<Task> Next)
            {
                Conn.Services.GetRequiredService<State>()
                    .LastRx = DateTime.Now;

                return Next.Invoke();
            }
        }

        /// <summary>
        /// Ping packet.
        /// </summary>
        [Packet(Name = "xnet.ping")]
        public class Ping : IPacket, IPacketHandler
        {
            /// <inheritdoc/>
            public void Encode(BinaryWriter Writer)
            {
            }

            /// <inheritdoc/>
            public void Decode(BinaryReader Reader)
            {
            }

            /// <inheritdoc/>
            public Task HandleAsync(Connection Conn, IPacket Packet, Func<Task> Next)
            {
                Console.WriteLine("PING");
                return Conn.EmitAsync(new Pong());
            }
        }

        /// <summary>
        /// Pong packet.
        /// </summary>
        [Packet(Name = "xnet.pong")]
        public class Pong : IPacket , IPacketHandler
        {
            /// <inheritdoc/>
            public void Encode(BinaryWriter Writer)
            {
            }

            /// <inheritdoc/>
            public void Decode(BinaryReader Reader)
            {
            }

            public Task HandleAsync(Connection Conn, IPacket Packet, Func<Task> Next)
            {
                Console.WriteLine("PONG");
                return Task.CompletedTask;
            }
        }
    }
}
