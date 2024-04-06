namespace Xnet.Packets.Impls
{
    internal class DelegateGreeter : IGreeter
    {
        private readonly Func<Connection, Func<Task>, Task> m_Greeter;

        /// <summary>
        /// Initialize a new <see cref="DelegateGreeter"/> instance.
        /// </summary>
        /// <param name="Greeter"></param>
        public DelegateGreeter(Func<Connection, Func<Task>, Task> Greeter)
            => m_Greeter = Greeter;

        /// <inheritdoc/>
        public Task GreetingAsync(Connection Connecton, Func<Task> Next)
        {
            return m_Greeter.Invoke(Connecton, Next);
        }
    }

}
