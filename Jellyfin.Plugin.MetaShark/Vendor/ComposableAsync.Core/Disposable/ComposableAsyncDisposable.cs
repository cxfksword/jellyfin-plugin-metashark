using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;

namespace ComposableAsync
{
    /// <summary>
    /// <see cref="IAsyncDisposable"/> implementation aggregating other <see cref="IAsyncDisposable"/>
    /// </summary>
    public sealed class ComposableAsyncDisposable : IAsyncDisposable
    {
        private readonly ConcurrentQueue<IAsyncDisposable> _Disposables;

        /// <summary>
        /// Build an empty ComposableAsyncDisposable
        /// </summary>
        public ComposableAsyncDisposable()
        {
            _Disposables = new ConcurrentQueue<IAsyncDisposable>();
        }

        /// <summary>
        /// Add an <see cref="IAsyncDisposable"/> to the ComposableAsyncDisposable
        /// and returns it
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="disposable"></param>
        /// <returns></returns>
        public T Add<T>(T disposable) where T: IAsyncDisposable
        {
            if (disposable == null)
                return default(T);

            _Disposables.Enqueue(disposable);
            return disposable;
        }

        /// <summary>
        /// Dispose all the resources asynchronously
        /// </summary>
        /// <returns></returns>
        public Task DisposeAsync()
        {
            var tasks = _Disposables.ToArray().Select(disposable => disposable.DisposeAsync()).ToArray();
            return Task.WhenAll(tasks);
        }
    }
}
