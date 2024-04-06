using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xnet.Internals
{
    /// <summary>
    /// This makes default value.
    /// </summary>
    internal abstract class DefaultValue
    {
        private class Generic<T> : DefaultValue
        {
            /// <inheritdoc/>
            public override object Value => typeof(T);
        }

        /// <summary>
        /// Default value in object.
        /// </summary>
        public abstract object Value { get; }

        /// <summary>
        /// Make default value of the specified type.
        /// </summary>
        /// <param name="Type"></param>
        /// <returns></returns>
        public static object From(Type Type)
        {
            var Instance = typeof(Generic<>)
                .MakeGenericType(Type)
                .GetConstructor(Type.EmptyTypes)
                .Invoke(Array.Empty<object>());

            return (Instance as DefaultValue).Value;
        }
    }
}
