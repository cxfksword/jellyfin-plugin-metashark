using System;
using System.Threading;
using System.Threading.Tasks;

namespace ComposableAsync
{
    internal class DispatcherAdapter : IDispatcher
    {
        private readonly IBasicDispatcher _BasicDispatcher;

        public DispatcherAdapter(IBasicDispatcher basicDispatcher)
        {
            _BasicDispatcher = basicDispatcher;
        }

        public IDispatcher Clone() => new DispatcherAdapter(_BasicDispatcher.Clone());

        public void Dispatch(Action action)
        {
            _BasicDispatcher.Enqueue(action, CancellationToken.None);
        }

        public Task Enqueue(Action action)
        {
            return _BasicDispatcher.Enqueue(action, CancellationToken.None);
        }

        public Task<T> Enqueue<T>(Func<T> action)
        {
            return _BasicDispatcher.Enqueue(action, CancellationToken.None);
        }

        public Task Enqueue(Func<Task> action)
        {
            return _BasicDispatcher.Enqueue(action, CancellationToken.None);
        }

        public Task<T> Enqueue<T>(Func<Task<T>> action)
        {
            return _BasicDispatcher.Enqueue(action, CancellationToken.None);
        }

        public Task<T> Enqueue<T>(Func<T> action, CancellationToken cancellationToken)
        {
            return _BasicDispatcher.Enqueue(action, cancellationToken);
        }

        public Task Enqueue(Action action, CancellationToken cancellationToken)
        {
            return _BasicDispatcher.Enqueue(action, cancellationToken);
        }

        public Task Enqueue(Func<Task> action, CancellationToken cancellationToken)
        {
            return _BasicDispatcher.Enqueue(action, cancellationToken);
        }

        public Task<T> Enqueue<T>(Func<Task<T>> action, CancellationToken cancellationToken)
        {
            return _BasicDispatcher.Enqueue(action, cancellationToken);
        }
    }
}