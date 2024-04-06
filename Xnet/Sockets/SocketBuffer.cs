using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xnet.Sockets
{
    /// <summary>
    /// Buffer that optimized to handle byte stream for sockets.
    /// </summary>
    internal partial class SocketBuffer
    {
        /// <summary>
        /// Chunk pool.
        /// </summary>
        private static readonly ArrayPool<byte> POOL = ArrayPool<byte>.Shared;

        /// <summary>
        /// Max size of a chunk.
        /// </summary>
        private const int FULL_CHUNK = 4096;
        private const int MIN_CHUNK = 256;

        /// <summary>
        /// Rent buffer.
        /// </summary>
        /// <returns></returns>
        public static byte[] Rent() => POOL.Rent(FULL_CHUNK);

        /// <summary>
        /// Return buffer.
        /// </summary>
        /// <param name="Buffer"></param>
        public static void Return(byte[] Buffer) => POOL.Return(Buffer);

        // --
        private Queue<Item> m_Queue;
        private Item m_LastItem;
        private int m_Length;
        private int m_Capacity;

        /// <summary>
        /// Initialize a new <see cref="SocketBuffer"/> instance.
        /// </summary>
        public SocketBuffer()
        {
            m_Queue = new();
            m_LastItem = null;
            m_Capacity = 0;
            m_Length = 0;
        }
        
        /// <summary>
        /// Length.
        /// </summary>
        public int Length { get { lock (this) return m_Length; } }

        /// <summary>
        /// Capacity.
        /// </summary>
        public int Capacity { get { lock (this) return m_Capacity; } }

        /// <summary>
        /// Clear the socket buffer.
        /// </summary>
        public void Clear()
        {
            lock (this)
            {
                while (m_Queue.TryDequeue(out var Each))
                {
                    Return(Each.Array);
                }

                m_Length = 0;
                m_LastItem = null;
            }
        }

        /// <summary>
        /// Optimize the socket buffer once.
        /// </summary>
        public void Optimize()
        {
            lock(this)
            {
                // --> no need to optimize.
                if (m_Capacity - m_Length < FULL_CHUNK)
                    return;

                var Backup = m_Queue;

                m_Queue = new Queue<Item>();
                m_LastItem = null;

                if (Backup.TryPeek(out var Head))
                {
                    m_Queue.Enqueue(Head);
                    m_LastItem = Backup.Dequeue();
                }

                MergeChunks(Backup, 0);
            }
        }

        /// <summary>
        /// Enqueue bytes to the socket buffer.
        /// </summary>
        /// <param name="Buffer"></param>
        public void Enqueue(ArraySegment<byte> Buffer)
        {
            lock(this)
            {
                if (Buffer.Array is null)
                    return;

                while (Buffer.Count > 0)
                {
                    if (m_LastItem is null)
                    {
                        m_LastItem = new Item()
                        {
                            Offset = 0,
                            Length = 0,
                            Array = Rent()
                        };

                        m_Capacity += m_LastItem.Array.Length;
                        m_Queue.Enqueue(m_LastItem);
                    }

                    var Avail = m_LastItem.Available();
                    if (Avail <= 0)
                    {
                        // --> new item required.
                        m_LastItem = null;
                        continue;
                    }

                    Buffer = Enqueue(Buffer, Avail);
                }
            }
        }

        /// <summary>
        /// Prepend the specified buffer to head.
        /// </summary>
        /// <param name="Buffer"></param>
        public void Prepend(ArraySegment<byte> Buffer)
        {
            lock(this)
            {
                if (m_Queue.Count <= 0)
                {
                    Enqueue(Buffer);
                    return;
                }

                var Backup = m_Queue;

                // --> set empty.
                m_Queue = new Queue<Item>();
                m_LastItem = null;

                Enqueue(Buffer);

                // --> if no need to optimize, just put chunks.
                if (m_Capacity - m_Length < FULL_CHUNK)
                {
                    while (Backup.TryDequeue(out var Each))
                    {
                        m_Queue.Enqueue(Each);
                        m_LastItem = Each;
                    }

                    return;
                }

                MergeChunks(Backup);
            }
        }

        /// <summary>
        /// Merge queued chunks to current buffer.
        /// </summary>
        /// <param name="Backup"></param>
        private void MergeChunks(Queue<Item> Backup, int MinChunk = MIN_CHUNK)
        {
            if (MinChunk <= 0)
                MinChunk = 1;

            while (Backup.TryPeek(out var Each))
            {
                if (Each.Length <= 0)
                {
                    Return(Each.Array);
                    Backup.Dequeue();

                    m_Capacity -= Each.Array.Length;
                    continue;
                }

                if (m_LastItem != null)
                {
                    var Avail = m_LastItem.Available();

                    // --> if the chunk has empty space that is larger than minimal capacity.
                    if (Avail >= MinChunk)
                    {
                        MergeToLastChunk(Each);
                        continue;
                    }
                }

                m_Queue.Enqueue(Each);
                m_LastItem = Backup.Dequeue();
            }
        }

        /// <summary>
        /// Merge a chunk to last chunk.
        /// </summary>
        /// <param name="Chunk"></param>
        private void MergeToLastChunk(Item Chunk)
        {
            int Avail = Math.Min(Chunk.Length, m_LastItem.Available());

            m_LastItem.Optimize();
            Array.Copy(Chunk.Array, Chunk.Offset,
                m_LastItem.Array, m_LastItem.Ending,
                Avail);

            m_LastItem.Length += Avail;

            Chunk.Offset += Avail;
            Chunk.Length -= Avail;
        }

        /// <summary>
        /// Try to dequeue bytes as many bytes as buffer length.
        /// If no sufficient bytes are queued, this will not dequeue anything.
        /// But if <paramref name="Fullmode"/> is true, this will dequeue as many as possible.
        /// </summary>
        /// <param name="Buffer"></param>
        /// <param name="Fullmode"></param>
        /// <returns></returns>
        public int TryDequeue(ArraySegment<byte> Buffer, bool Fullmode = true)
        {
            lock(this)
            {
                if (Buffer.Array is null)
                    return 0;

                if (Fullmode && m_Length < Buffer.Count)
                    return 0;

                var Count = Buffer.Count;
                while (Buffer.Count > 0)
                {
                    var Head = m_Queue.Peek();
                    if (Head.Length <= 0)
                    {
                        // --> if this item is empty and holds no bytes.
                        if (Head == m_LastItem)
                            m_LastItem = null;

                        m_Capacity -= Head.Array.Length;
                        m_Queue.Dequeue();

                        Return(Head.Array);

                        continue;
                    }

                    Buffer = Dequeue(Head, Buffer);
                }

                return Count - Buffer.Count;
            }
        }

        /// <summary>
        /// Enqueue N bytes to the socket buffer.
        /// </summary>
        /// <param name="Buffer"></param>
        /// <param name="Length"></param>
        /// <returns></returns>
        private ArraySegment<byte> Enqueue(ArraySegment<byte> Buffer, int Length)
        {
            m_LastItem.Optimize();
            Length = Math.Min(Length, Buffer.Count);

            // --> put buffer to ending of last item. 
            Buffer.Slice(0, Length)
                .CopyTo(m_LastItem.Array, m_LastItem.Ending);

            m_LastItem.Length += Length;

            if (m_LastItem.Available() <= 0)
                m_LastItem = null;

            // --> accumulate total length of buffer.
            m_Length += Length;

            // --> skip handled length.
            return Buffer.Slice(Length);
        }

        /// <summary>
        /// Dequeue bytes from the heading item.
        /// </summary>
        /// <param name="Head"></param>
        /// <param name="Buffer"></param>
        /// <returns></returns>
        private ArraySegment<byte> Dequeue(Item Head, ArraySegment<byte> Buffer)
        {
            var Avail = Head.ToArraySegment();
            var Slice = Math.Min(Buffer.Count, Avail.Count);

            // --> copy bytes to the buffer,
            Avail.Slice(0, Slice).CopyTo(Buffer);

            // --> then, step forward.
            Head.Offset += Slice;
            Head.Length -= Slice;

            m_Length -= Slice;

            return Buffer.Slice(Slice);
        }

    }
}
