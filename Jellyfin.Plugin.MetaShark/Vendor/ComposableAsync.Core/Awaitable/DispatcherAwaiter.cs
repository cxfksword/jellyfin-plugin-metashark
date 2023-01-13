using System;
using System.Runtime.CompilerServices;
using System.Security;

namespace ComposableAsync
{
    /// <summary>
    /// Dispatcher awaiter, making a dispatcher awaitable
    /// </summary>
    public struct DispatcherAwaiter : INotifyCompletion
    {
        /// <summary>
        /// Dispatcher never is synchronous
        /// </summary>
        public bool IsCompleted => false;

        private readonly IDispatcher _Dispatcher;

        /// <summary>
        /// Construct a NotifyCompletion fom a dispatcher
        /// </summary>
        /// <param name="dispatcher"></param>
        public DispatcherAwaiter(IDispatcher dispatcher)
        {
            _Dispatcher = dispatcher;
        }

        /// <summary>
        /// Dispatch on complete
        /// </summary>
        /// <param name="continuation"></param>
        [SecuritySafeCritical]
        public void OnCompleted(Action continuation)
        {
            _Dispatcher.Dispatch(continuation);
        }

        /// <summary>
        /// No Result
        /// </summary>
        public void GetResult() { }
    }
}
