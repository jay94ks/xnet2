using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using Xnet.Packets;
using Xnet.Packets.Impls;

namespace Xnet.Internals
{
    internal static class Helpers
    {
        /// <summary>
        /// Create a type with dependency injection.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="Services"></param>
        /// <returns></returns>
        public static T Create<T>(this IServiceProvider Services, Type Type = null)
        {
            if (Type is null)
                Type = typeof(T);

            if (Type.IsAssignableTo(typeof(T)) == false)
                throw new InvalidOperationException("the specified type can not be assigned to T.");

            if (Type.IsAbstract)
                throw new InvalidOperationException("abstract types can not be instantiated.");

            var Ctors = Type.GetConstructors();
            if (Ctors.Length <= 0)
                throw new InvalidOperationException("the type has no any constructor.");

            var Selection = Ctors
                .Select(X => (Ctor: X, Params: X.GetParameters()))
                .Where(CheckValueParameters)
                .OrderByDescending(X => X.Params.Length)
                .FirstOrDefault();

            if (Selection.Ctor is null)
                throw new InvalidOperationException("the type has no compatible constructor.");

            var Parameters = Selection.Params.Select(Info =>
            {
                var ParamType = Info.ParameterType;
                if (ParamType.IsValueType == false || ParamType.IsInterface == true)
                    return Services.GetRequiredService(ParamType);

                if (Info.HasDefaultValue)
                    return Info.DefaultValue;

                return DefaultValue.From(ParamType);
            });

            return (T)Selection.Ctor.Invoke(Parameters.ToArray());
        }

        /// <summary>
        /// Check all value parameters.
        /// </summary>
        /// <param name="Info"></param>
        /// <returns></returns>
        private static bool CheckValueParameters((ConstructorInfo Ctor, ParameterInfo[] Params) Info)
        {
            var ValueTypes = Info.Params.Where(X => X.ParameterType.IsValueType);
            foreach (var Each in ValueTypes)
            {
                if (Each.ParameterType.IsInterface)
                    continue;

                return false;
            }

            return true;
        }

        /// <summary>
        /// Ensure the task flow to be asynchronous.
        /// </summary>
        /// <returns></returns>
        public static async Task EnsureAsync()
        {
            try
            {
                await Task.Yield();
                await Task.Delay(0);
            }
            catch { }
        }

        /// <summary>
        /// Get enumerable services.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="Services"></param>
        /// <returns></returns>
        public static IEnumerable<T> GetEnumerableServices<T>(this IServiceProvider Services) where T : class
        {
            var Result = Services.GetService<IEnumerable<T>>();
            if (Result is null)
            {
                Result = Array.Empty<T>();

                var Single = Services.GetService<T>();
                if (Single != null)
                    Result = Result.Append(Single);
            }

            return Result;
        }

        /// <summary>
        /// Invoke a <typeparamref name="T"/> pipeline.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="Pipeline"></param>
        /// <param name="Context"></param>
        /// <param name="Invoker"></param>
        /// <param name="Next"></param>
        /// <returns></returns>
        public static Task Pipeline<T>(
            this IEnumerable<T> Pipeline,
            Func<T, Func<Task>, Task> Invoker,
            Func<Task> Next = null)
        {
            var Queue = new Queue<T>(Pipeline);
            Task NextAsync()
            {
                if (Queue.TryDequeue(out var Current))
                    return Invoker.Invoke(Current, NextAsync);

                if (Next is null)
                    return Task.CompletedTask;

                return Next.Invoke();
            }

            return NextAsync();
        }

        /// <summary>
        /// Get packet handlers that injected (or itself) as attribute from packet.
        /// </summary>
        /// <param name="Packet"></param>
        /// <returns></returns>
        public static IEnumerable<IPacketHandler> GetHandlersIfPossible(this IPacket Packet)
        {
            var Attributes = Packet.GetType().GetCustomAttributes<PacketHandlerAttribute>();
            foreach (var Each in Attributes)
                yield return Each;

            if (Packet is IPacketHandler PacketHandler)
                yield return PacketHandler;

            var Type = Packet.GetType();
            var Interface = typeof(IPacketHandler<>).MakeGenericType(Type);
            if (Type.IsAssignableTo(Interface) == false)
                yield break;

            yield return typeof(GenericAdapter<>)
                    .MakeGenericType(Type).GetConstructors().First()
                    .Invoke(new object[] { Packet }) as IPacketHandler;
        }
    }
}
