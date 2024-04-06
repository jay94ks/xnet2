using System.Net;
using Xnet.Packets;

namespace Xnet.Example
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var Builder = WebApplication.CreateBuilder();

            Builder.WebHost.UseUrls("http://127.0.0.1:5000/");
            Builder.Services.AddXnet(Xnet =>
            {
                Xnet.Listen(new IPEndPoint(IPAddress.Any, 4700));
                Xnet.Connect("127.0.0.1", 4700);

                Xnet.UseMapper(Mapper => {
                    Mapper
                        .Map<Foo>()
                        .Map<Bar>();
                });

                Xnet.UseGreeter(async (Conn, Next) =>
                {
                    await Conn.EmitAsync(new Foo());
                    await Next.Invoke();
                });

                Xnet.UseHandler<Foo>((Conn, Packet, Next) =>
                {
                    Console.WriteLine($"[{Conn.RemoteEndpoint}]: Foo");
                    return Next.Invoke();
                });

                Xnet.UseHandler<Bar>((Conn, Packet, Next) =>
                {
                    Console.WriteLine($"[{Conn.RemoteEndpoint}]: Bar");
                    return Next.Invoke();
                });
            });

            Builder.Build().Run();
        }
    }

    [Packet(Name = "example.foo")]
    class Foo : IPacket, IPacketHandler<Foo>
    {
        public void Encode(BinaryWriter Writer)
        {
            // ... Write payloads here ... 
        }
        public void Decode(BinaryReader Reader)
        {
            // ... Read payloads here ...
        }

        public Task HandleAsync(Connection Conn, Foo Packet, Func<Task> Next)
        {
            return Conn.EmitAsync(new Bar());
        }
    }

    [Packet(Name = "example.bar")]
    class Bar : IPacket, IPacketHandler<Bar>
    {
        public void Encode(BinaryWriter Writer)
        {
            // ... Write payloads here ... 
        }
        public void Decode(BinaryReader Reader)
        {
            // ... Read payloads here ...
        }

        public Task HandleAsync(Connection Conn, Bar Packet, Func<Task> Next)
        {
            return Next.Invoke();
        }
    }
}