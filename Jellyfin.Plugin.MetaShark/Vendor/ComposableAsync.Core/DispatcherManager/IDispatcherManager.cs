namespace ComposableAsync
{
    /// <summary>
    /// Dispatcher manager
    /// </summary>
    public interface IDispatcherManager : IAsyncDisposable
    {
        /// <summary>
        /// true if the Dispatcher should be released
        /// </summary>
        bool DisposeDispatcher { get; }

        /// <summary>
        /// Returns a consumable Dispatcher
        /// </summary>
        /// <returns></returns>
        IDispatcher GetDispatcher();
    }
}
