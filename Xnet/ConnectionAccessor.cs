namespace Xnet
{
    /// <summary>
    /// Connection accessor.
    /// </summary>
    public class ConnectionAccessor
    {
        private readonly AsyncLocal<Connection> m_Current = new();

        /// <summary>
        /// Access to the current instance.
        /// </summary>
        public Connection Current => m_Current.Value;

        /// <summary>
        /// Set the current connection and returns the previous.
        /// </summary>
        /// <param name="Conn"></param>
        /// <returns></returns>
        internal Connection SetCurrent(Connection Conn)
        {
            var Prev = m_Current.Value;
            m_Current.Value = Conn;
            return Prev;
        }
    }
}
