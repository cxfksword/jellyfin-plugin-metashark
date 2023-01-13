using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;

namespace ComposableAsync
{
    /// <summary>
    /// <see cref="IDispatcher"/> extension methods provider
    /// </summary>
    public static class DispatcherExtension
    {
        /// <summary>
        /// Returns awaitable to enter in the dispatcher context
        /// This extension method make a dispatcher awaitable
        /// </summary>
        /// <param name="dispatcher"></param>
        /// <returns></returns>
        public static DispatcherAwaiter GetAwaiter(this IDispatcher dispatcher)
        {
            return new DispatcherAwaiter(dispatcher);
        }

        /// <summary>
        /// Returns a composed dispatcher applying the given dispatcher
        /// after the first one
        /// </summary>
        /// <param name="dispatcher"></param>
        /// <param name="other"></param>
        /// <returns></returns>
        public static IDispatcher Then(this IDispatcher dispatcher, IDispatcher other)
        {
            if (dispatcher == null)
                throw new ArgumentNullException(nameof(dispatcher));

            if (other == null)
                throw new ArgumentNullException(nameof(other));

            return new ComposedDispatcher(dispatcher, other);
        }

        /// <summary>
        /// Returns a composed dispatcher applying the given dispatchers sequentially
        /// </summary>
        /// <param name="dispatcher"></param>
        /// <param name="others"></param>
        /// <returns></returns>
        public static IDispatcher Then(this IDispatcher dispatcher, params IDispatcher[] others)
        {
            return dispatcher.Then((IEnumerable<IDispatcher>)others);
        }

        /// <summary>
        /// Returns a composed dispatcher applying the given dispatchers sequentially
        /// </summary>
        /// <param name="dispatcher"></param>
        /// <param name="others"></param>
        /// <returns></returns>
        public static IDispatcher Then(this IDispatcher dispatcher, IEnumerable<IDispatcher> others)
        {
            if (dispatcher == null)
                throw new ArgumentNullException(nameof(dispatcher));

            if (others == null)
                throw new ArgumentNullException(nameof(others));

            return others.Aggregate(dispatcher, (cum, val) => cum.Then(val));
        }

        /// <summary>
        /// Create a <see cref="DelegatingHandler"/> from an <see cref="IDispatcher"/>
        /// </summary>
        /// <param name="dispatcher"></param>
        /// <returns></returns>
        public static DelegatingHandler AsDelegatingHandler(this IDispatcher dispatcher)
        {
            return new DispatcherDelegatingHandler(dispatcher);
        }

        /// <summary>
        /// Create a <see cref="IDispatcher"/> from an <see cref="IBasicDispatcher"/>
        /// </summary>
        /// <param name="basicDispatcher"></param>
        /// <returns></returns>
        public static IDispatcher ToFullDispatcher(this IBasicDispatcher @basicDispatcher)
        {
            return new DispatcherAdapter(@basicDispatcher);
        }
    }
}
