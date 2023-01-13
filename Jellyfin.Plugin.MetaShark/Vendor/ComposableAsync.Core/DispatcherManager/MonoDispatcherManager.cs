using System.Threading.Tasks;

namespace ComposableAsync
{
    /// <summary>
    /// <see cref="IDispatcherManager"/> implementation based on single <see cref="IDispatcher"/>
    /// </summary>
    public sealed class MonoDispatcherManager : IDispatcherManager
    {
        /// <inheritdoc cref="IDispatcherManager"/>
        public bool DisposeDispatcher { get; }

        /// <inheritdoc cref="IDispatcherManager"/>
        public IDispatcher GetDispatcher() => _Dispatcher;

        private readonly IDispatcher _Dispatcher;

        /// <summary>
        /// Create 
        /// </summary>
        /// <param name="dispatcher"></param>
        /// <param name="shouldDispose"></param>
        public MonoDispatcherManager(IDispatcher dispatcher, bool shouldDispose = false)
        {
            _Dispatcher = dispatcher;
            DisposeDispatcher = shouldDispose;
        }

        /// <inheritdoc cref="IDispatcherManager"/>
        public Task DisposeAsync()
        {
            return DisposeDispatcher && (_Dispatcher is IAsyncDisposable disposable) ?
                disposable.DisposeAsync() : Task.CompletedTask;
        }
    }
}
