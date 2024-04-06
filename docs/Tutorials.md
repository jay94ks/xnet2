# Configuration.
## Creating a project
This project runs over the `Microsoft.Extensions.Hosting` package.
So, all of those kind projects are compatible.

In this part, we will use `ASP.NET core` project.
And you can refer [Microsoft's official tutorial](https://learn.microsoft.com/en-us/visualstudio/get-started/csharp/tutorial-aspnet-core?view=vs-2022) for this part.
But as exceptionally, I recommend you that use project self-hosting instead of `IIS Express`.

## Copy `Xnet` library and Adding `Xnet` dependencies.
I recommend you that all project files to your solution directory
to customize `Xnet` library easily for against various situation.
Ofcourse, you can use `nuget` packages.

```
Your-Solution
  + Xnet (folder)
     - Xnet (core library)
     - place extension library projects here.

  + Your-Project.
```
And, after copying library projects, your solution tree will seem like above.
Then, add references to your shared library or executable project directly.

## Configuring the `Xnet` library.
Open your `Startup.cs` file to add configuration of `Xnet` library.

```
var HostBuilder = WebApplication.CreateBuilder();

// --> to enable client features only, no delegate is needed.
HostBuilder.Services.AddXnet();

// --> to open `Xnet` server on TCP 4700:
HostBuilder.Services.AddXnet(Xnet => {
    Xnet.Listen(new IPEndPoint(IPAddress.Any, 4700));

    // --> to add initial peer (if you want to implement P2P features):
    Xnet.Connect("xnet.example.com", 4700);
});
```

Currently, `Xnet` framework supports only `tcp` based.
but more protocols will be implemented in future.

## Implementing `Foo` and `Bar` packets.
In here, we will implement `Foo` packet and emit it on connection is started.
then, replys `Bar` for incoming `Foo`.

```
[Packet(Name = "example.foo")]
class Foo : IPacket, IPacketHandler<Foo> {
    public void Encode(BinaryWriter Writer) { 
        // ... Write payloads here ... 
    }
    public void Decode(BinaryReader Reader) { 
        // ... Read payloads here ...
    }

    public Task HandleAsync(Connection Conn, Foo Packet, Func<Task> Next) {
        return Conn.EmitAsync(new Bar());
    }
}

[Packet(Name = "example.bar")]
class Bar : IPacket, IPacketHandler<Bar> {
    public void Encode(BinaryWriter Writer) { 
        // ... Write payloads here ... 
    }
    public void Decode(BinaryReader Reader) { 
        // ... Read payloads here ...
    }

    public Task HandleAsync(Connection Conn, Bar Packet, Func<Task> Next) {
        return Next.Invoke();
    }
}
```

Now, we have two packet classes. So, map them to `Xnet` library like:
```
HostBuilder.Services.AddXnet(Xnet => {
    // ...

    Xnet.UseMapper(Mapper => {
        Mapper
            .Map<Foo>()
            .Map<Bar>();

        // or map entire the assembly.
    });
});
```