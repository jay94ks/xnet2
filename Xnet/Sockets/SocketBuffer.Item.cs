namespace Xnet.Sockets
{
    internal partial class SocketBuffer
    {
        /// <summary>
        /// An item that holds buffered bytes.
        /// </summary>
        private class Item
        {
            /// <summary>
            /// Offset.
            /// </summary>
            public int Offset;

            /// <summary>
            /// Length.
            /// </summary>
            public int Length;

            /// <summary>
            /// Array.
            /// </summary>
            public byte[] Array;

            /// <summary>
            /// End offset.
            /// </summary>
            public int Ending => Offset + Length;

            /// <summary>
            /// Compute available length of this item.
            /// </summary>
            /// <returns></returns>
            public int Available()
            {
                if (Array is null)
                    return 0;

                return Array.Length - Length;
            }

            /// <summary>
            /// Optimize space of an item.
            /// </summary>
            public void Optimize()
            {
                // --> if the offset is not zero,
                //   : make it to be zero.
                if (Offset > 0)
                {
                    System.Array.Copy(
                        Array, Offset,
                        Array, 0, Length);

                    Offset = 0;
                }
            }

            /// <summary>
            /// Make an array segment.
            /// </summary>
            /// <returns></returns>
            public ArraySegment<byte> ToArraySegment()
            {
                if (Array is null)
                    return System.Array.Empty<byte>();

                return new ArraySegment<byte>(Array, Offset, Length);
            }
        }
    }
}
