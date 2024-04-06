namespace Xnet.Packets
{
    /// <summary>
    /// Greeter interface.
    /// </summary>
    public interface IGreeter
    {
        /// <summary>
        /// Greeting the connection.
        /// If no <paramref name="Next"/> call exists,
        /// it will terminate the connection immediately.
        /// </summary>
        /// <param name="Connecton"></param>
        /// <param name="Next"></param>
        /// <returns></returns>
        Task GreetingAsync(Connection Connecton, Func<Task> Next);
    }
}
