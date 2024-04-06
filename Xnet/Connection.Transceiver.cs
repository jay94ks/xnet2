using System.Buffers.Binary;
using System.Diagnostics;
using System.Text;
using Xnet.Internals;
using Xnet.Packets;
using Xnet.Sockets;

namespace Xnet
{
    public sealed partial class Connection
    {

        /// <summary>
        /// Length of frame header.
        /// 1. Magic (2 byte).
        /// 2. Length (2 byte).
        /// 3. GUID (16 byte).
        /// </summary>
        private const int HEADER_LEN = sizeof(ushort) * 2 + 16;

        /// <summary>
        /// Magic value to recognize packets.
        /// </summary>
        private const ushort MAGIC_VALUE = 0xCAFE;

        /// <summary>
        /// Receiver state.
        /// </summary>
        enum State
        {
            WAIT_HEADER = 0,
            WAIT_PAYLOAD,
            DECODE_MESSAGE
        }

        // ---
        private readonly SocketBuffer m_RecvBuf = new();
        private State m_State = State.WAIT_HEADER;

        private ushort m_RecvMagic = 0;
        private ushort m_RecvLength = 0;
        private Guid m_RecvGuid = Guid.Empty;

        /// <summary>
        /// Run the receiver loop.
        /// </summary>
        /// <returns></returns>
        internal async Task RunLoop()
        {
            var Header = new byte[HEADER_LEN];
            var Payload = Array.Empty<byte>();
            var Queue = new Queue<IPacket>();


            while (Closing.IsCancellationRequested == false)
            {
                switch (m_State)
                {
                    case State.WAIT_HEADER:
                        if (TryInterpretHeader(Header, ref Payload) == false)
                            break; // --> more bytes required.

                        continue;

                    case State.WAIT_PAYLOAD:
                        if (TryWaitForPayload(Payload) == false)
                            break; // --> more bytes required.

                        continue;

                    case State.DECODE_MESSAGE:
                        {
                            var Packet = TryDecodePacket(m_RecvGuid, Payload);
                            if (Packet != null)
                                Queue.Enqueue(Packet);

                            m_State = State.WAIT_HEADER;
                        }
                        continue;
                }

                while (Queue.TryDequeue(out var Packet))
                    await HandleAsync(Packet);

                await ReceiveAsync();
            }
        }
        
        /// <summary>
        /// Try to interpret header.
        /// </summary>
        /// <param name="Header"></param>
        /// <param name="Payload"></param>
        /// <returns></returns>
        private bool TryInterpretHeader(byte[] Header, ref byte[] Payload)
        {
            var Len = m_RecvBuf.TryDequeue(Header);
            if (Len < Header.Length)
                return false; // --> more bytes required.

            m_RecvMagic = BinaryPrimitives.ReadUInt16LittleEndian(Header.AsSpan(0, 2));
            m_RecvLength = BinaryPrimitives.ReadUInt16LittleEndian(Header.AsSpan(2, 2));
            m_RecvGuid = new Guid(Header.AsSpan(4, 16));
            m_State = State.WAIT_PAYLOAD;

            // --> invalid magic number: disconnect.
            if (m_RecvMagic != MAGIC_VALUE)
            {
                Kick();
                return true;
            }

            if (Payload.Length != m_RecvLength)
                Array.Resize(ref Payload, m_RecvLength);

            return true;
        }

        /// <summary>
        /// Try to wait for payload to be ready.
        /// </summary>
        /// <param name="Payload"></param>
        /// <returns></returns>
        private bool TryWaitForPayload(byte[] Payload)
        {
            var Len = m_RecvBuf.TryDequeue(Payload);
            if (Len < Payload.Length)
                return false; // --> more bytes required.

            m_State = State.DECODE_MESSAGE;
            return true;
        }

        /// <summary>
        /// Try to decode the packet.
        /// </summary>
        /// <param name="PacketId"></param>
        /// <param name="Payload"></param>
        /// <returns></returns>
        private IPacket TryDecodePacket(Guid PacketId, byte[] Payload)
        {
            foreach (var Decoder in m_Providers)
            {
                var Packet = Decoder.TryCreate(PacketId);
                if (Packet is null)
                    continue;

                return Decode(Packet, Payload);
            }

            Kick();
            return null;
        }

        /// <summary>
        /// Decode the packet.
        /// </summary>
        /// <param name="Instance"></param>
        /// <param name="Payload"></param>
        /// <returns></returns>
        private IPacket Decode(IPacket Instance, byte[] Payload)
        {
            using var Stream = new MemoryStream(Payload, false);
            using var Reader = new BinaryReader(Stream, Encoding.UTF8, true);

            if (Debugger.IsAttached)
            {
                Instance.Decode(Reader);
                return Instance;
            }

            try
            {
                Instance.Decode(Reader);
                return Instance;
            }
            catch
            {
            }

            Kick();
            return null;
        }

        /// <summary>
        /// Receive and enqueue bytes to the buffer.
        /// </summary>
        /// <returns></returns>
        private async ValueTask<bool> ReceiveAsync()
        {
            var Buffer = SocketBuffer.Rent();
            try
            {
                var Length = await m_Socket.ReceiveAsync(Buffer);
                if (Length <= 0)
                    return false;

                m_RecvBuf.Enqueue(new(Buffer, 0, Length));
                return true;
            }
            finally
            {
                SocketBuffer.Return(Buffer);
            }
        }

        /// <summary>
        /// Encode the packet into byte array.
        /// </summary>
        /// <param name="Packet"></param>
        /// <returns></returns>
        private byte[] Encode(IPacket Packet)
        {
            foreach (var Encoder in m_Providers)
            {
                if (Encoder.TryGetPacketId(Packet, out var PacketId) == false)
                    continue;

                using var Stream = new MemoryStream();
                EnsureHeader(Stream, PacketId);

                var Remind = Stream.Position;
                using var Writer = new BinaryWriter(Stream, Encoding.UTF8, true);

                if (Debugger.IsAttached)
                    Packet.Encode(Writer);

                else
                {
                    try { Packet.Encode(Writer); }
                    catch
                    {
                        Kick();
                        return null;
                    }
                }

                EnsureHeader(Stream, PacketId, (ushort)(Stream.Position - Remind));
                return Stream.ToArray();
            }

            return null;
        }

        /// <summary>
        /// Write header bytes to the stream.
        /// </summary>
        /// <param name="PacketId"></param>
        /// <param name="Stream"></param>
        private static void EnsureHeader(MemoryStream Stream, Guid PacketId, ushort Length = 0)
        {
            Span<byte> Header = stackalloc byte[HEADER_LEN];

            BinaryPrimitives.WriteUInt16LittleEndian(Header.Slice(0, 2), MAGIC_VALUE);
            BinaryPrimitives.WriteUInt16LittleEndian(Header.Slice(2, 2), Length);
            PacketId.TryWriteBytes(Header.Slice(4, 16));

            // --> make placeholder to store headers.
            Stream.Seek(0, SeekOrigin.Begin);
            Stream.Write(Header);
        }

        /// <summary>
        /// Emit a packet to the remote host asynchronously.
        /// </summary>
        /// <param name="Packet"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        public async Task<bool> EmitAsync(IPacket Packet, CancellationToken Token = default)
        {
            var Bytes = Encode(Packet);
            if (Bytes is null)
                return false;

            try { await m_EmitLock.WaitAsync(Token); }
            catch
            {
                return false;
            }
            try
            {
                var Segment = new ArraySegment<byte>(Bytes);
                while (Segment.Count > 0)
                {
                    var Length = await m_Socket.SendAsync(Segment);
                    if (Length <= 0)
                        break;

                    Segment = Segment.Slice(Length);
                }

                return Segment.Count <= 0;
            }

            finally
            {
                try { m_EmitLock.Release(); }
                catch
                {
                }
            }
        }

        /// <summary>
        /// Handle the packet asynchronously.
        /// </summary>
        /// <param name="Packet"></param>
        /// <returns></returns>
        private Task HandleAsync(IPacket Packet)
        {
            var Extras = Packet.GetHandlersIfPossible();
            return m_Handlers.Concat(Extras).Pipeline(
                (Handler, Next) => Handler.HandleAsync(this, Packet, Next));
        }
    }
}