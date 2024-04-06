using System.Linq.Expressions;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace Xnet.Packets
{
    /// <summary>
    /// Packet provider builder.
    /// </summary>
    public sealed class PacketProviderBuilder
    {
        private readonly Dictionary<Type, Guid> m_Mappings = new();
        private readonly Dictionary<Guid, Func<IPacket>> m_Ctors = new();

        /// <summary>
        /// Make a packet id.
        /// </summary>
        /// <param name="Name"></param>
        /// <returns></returns>
        private static Guid MakeId(string Name)
        {
            var RawName = Encoding.UTF8.GetBytes($"PACKET [{Name}]");
            using var Md5 = MD5.Create();
            return new Guid(Md5.ComputeHash(RawName));
        }

        /// <summary>
        /// Build a packet provider.
        /// </summary>
        /// <returns></returns>
        public IPacketProvider Build()
        {
            return new PacketProvider(m_Mappings, m_Ctors);
        }

        /// <summary>
        /// Map all packets in the assembly.
        /// </summary>
        /// <param name="Assembly"></param>
        /// <param name="Filter"></param>
        /// <param name="Prefix"></param>
        /// <returns></returns>
        public PacketProviderBuilder Map(Assembly Assembly, Func<Type, bool> Filter = null, string Prefix = null)
        {
            var Types = Assembly.GetTypes()
                .Where(X => X.IsAbstract == false)
                .Where(X => X.IsAssignableTo(typeof(IPacket)))
                .Where(X => X.GetConstructor(Type.EmptyTypes) != null)
                .Where(X => X.GetCustomAttribute<PacketAttribute>() != null)
                ;

            if (Filter != null)
                Types = Types.Where(Filter);

            foreach (var Type in Types)
                MapUnchecked(Type, Prefix, null);

            return this;
        }

        /// <summary>
        /// Map all packets in the collection.
        /// </summary>
        /// <param name="Collection"></param>
        /// <param name="Prefix"></param>
        /// <returns></returns>
        public PacketProviderBuilder Map(IEnumerable<Type> Collection, string Prefix = null)
        {
            var Types = Collection
                .Where(X => X.IsAbstract == false)
                .Where(X => X.IsAssignableTo(typeof(IPacket)))
                .Where(X => X.GetConstructor(Type.EmptyTypes) != null)
                .Where(X => X.GetCustomAttribute<PacketAttribute>() != null)
                ;

            foreach (var Type in Types)
                MapUnchecked(Type, Prefix, null);

            return this;
        }

        /// <summary>
        /// Map the type as packet.
        /// </summary>
        /// <param name="Type"></param>
        /// <param name="Name"></param>
        /// <returns></returns>
        public PacketProviderBuilder Map(Type Type, string Name = null)
        {
            if (Type is null)
                throw new ArgumentNullException(nameof(Type));

            if (Type.IsAbstract)
                throw new ArgumentException("the packet type must not be abstract.");

            if (Type.IsAssignableTo(typeof(IPacket)) == false)
                throw new ArgumentException("the packet type must implement `IPacket` interface.");

            return MapUnchecked(Type, null, Name);
        }

        /// <summary>
        /// Map the type as packet.
        /// </summary>
        /// <typeparam name="TPacket"></typeparam>
        /// <param name="Name"></param>
        /// <returns></returns>
        public PacketProviderBuilder Map<TPacket>(string Name = null) where TPacket : IPacket, new()
        {
            return Map(typeof(TPacket), Name);
        }

        /// <summary>
        /// Map the type as packet with name.
        /// </summary>
        /// <param name="Type"></param>
        /// <param name="Name"></param>
        /// <returns></returns>
        /// <exception cref="NotSupportedException"></exception>
        private PacketProviderBuilder MapUnchecked(Type Type, string Prefix, string Name)
        {
            var Ctor = Type.GetConstructor(Type.EmptyTypes);
            if (Ctor is null)
                throw new NotSupportedException("the packet type must have default constructor.");

            var Lambda = Expression
                .Lambda<Func<IPacket>>(Expression.New(Ctor))
                .Compile();

            if (string.IsNullOrWhiteSpace(Name))
            {
                var Attr = Type.GetCustomAttribute<PacketAttribute>();
                if (Attr != null)
                    Name = Attr.Name;

                if (string.IsNullOrWhiteSpace(Name))
                    Name = Type.FullName;
            }

            if (string.IsNullOrWhiteSpace(Prefix) == false)
                Name = $"{Prefix}{Name}";

            var Guid = MakeId(Name);

            if (m_Mappings.TryAdd(Type, Guid))
                m_Ctors[Guid] = Lambda;

            return this;
        }
    }
}
