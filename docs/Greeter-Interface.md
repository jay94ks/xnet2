# Greeter interface.
This is to handle the connection's head(or tail) behaviours.
The flow of connections are:
```
1. Accepted and encapsulated into connection object.
2. Greeter pipeline.
3. Packet receiver.
```

And sending packets will lock the async flow using semaphore (only send channel).
The main execution method is very similar to [the middleware pipelining of `ASP.NET core`](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/middleware/?view=aspnetcore-8.0), 
so I don't think it will be difficult to understand.

## Connection management greeter
The main example of the `Greeter` interface is probably `ConnectionManager`. 
In this example, you can expose the access point for each connection in the logic managed by `DI`.

```
class ConnectionManagerGreeter : IGreeter {
	public async Task GreetingAsync(Connection Conn, Func<Task> Next) {
		var Manager = Connection.Services.GetRequiredService<ConnectionManager>();
		if (Manager.AddConnection(Conn) == false) {
			return;
		}

		var Prev = Manager.SetCurrent(Conn);
		try { await Next.Invoke(); } // --> if you don't call this, the connection will be kicked immediately.
		finally {
			Manager.RemoveConnection(Conn);
			Manager.SetCurrent(Prev);
		}
	}
}

class ConnectionManager {
	private readonly HashSet<Connection> m_Conns = new();
	private readonly AsyncLocal<Connection> m_Current = new();

	public Connection Current => m_Current.Value;

	internal bool AddConnection(Connection Conn) {
		if (/* filter condition */) {
			return false;
		}

		lock (m_Conns) {
			return m_Conns.Add(Conn);
		}
	}

	internal void RemoveConnection(Connection Conn) {
		lock (m_Conns) {
			m_Conns.Remove(Conn);
		}
	}

	internal Connection SetCurrent(Connection Conn) {
		var Prev = m_Current.Value;
		m_Current.Value = Conn;
		return Prev;
	}

	public Connection[] Snapshot() {
		lock (m_Conns) {
			return m_Conns.ToArray();
		}
	}
}
```

And, this can be configured like this:
```
HostBuilder.Services.AddXnet(Xnet => {
    // ...

    Xnet.UseGreeter<ConnectionManagerGreeter>();

	// --> or you can use pre-instantiated greeter:
	Xnet.UseGreeter(new ConnectionManagerGreeter>();
});
```