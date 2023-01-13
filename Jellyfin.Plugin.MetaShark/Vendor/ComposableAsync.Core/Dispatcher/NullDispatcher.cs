using System;
using System.Threading;
using System.Threading.Tasks;

namespace ComposableAsync
{
    /// <summary>
    /// <see cref="IDispatcher"/> that run actions synchronously
    /// </summary>
    public sealed class NullDispatcher: IDispatcher
    {
        private NullDispatcher() { }

        /// <summary>
        /// Returns a static null dispatcher
        /// </summary>
        public static IDispatcher Instance { get; } = new NullDispatcher();

        /// <inheritdoc />
        public void Dispatch(Action action)
        {
            action();
        }

        /// <inheritdoc />
        public Task Enqueue(Action action)
        {
            action();
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task<T> Enqueue<T>(Func<T> action)
        {
            return Task.FromResult(action());
        }

        /// <inheritdoc />
        public async Task Enqueue(Func<Task> action)
        {
            await action();
        }

        /// <inheritdoc />
        public async Task<T> Enqueue<T>(Func<Task<T>> action)
        {
            return await action();
        }

        public Task<T> Enqueue<T>(Func<T> action, CancellationToken cancellationToken)
        {
            return Task.FromResult(action());
        }

        public Task Enqueue(Action action, CancellationToken cancellationToken)
        {
            action();
            return Task.CompletedTask;
        }

        public async Task Enqueue(Func<Task> action, CancellationToken cancellationToken)
        {
            await action();
        }

        public async Task<T> Enqueue<T>(Func<Task<T>> action, CancellationToken cancellationToken)
        {
            return await action();
        }

        /// <inheritdoc />
        public IDispatcher Clone() => Instance;
    }
}
