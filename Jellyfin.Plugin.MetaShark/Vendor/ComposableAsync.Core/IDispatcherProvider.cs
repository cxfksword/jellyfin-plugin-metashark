namespace ComposableAsync
{
    /// <summary>
    /// Returns the fiber associated with an actor
    /// </summary>
    public interface IDispatcherProvider
    {
        /// <summary>
        /// Returns the corresponding <see cref="IDispatcher"/>
        /// </summary>
        IDispatcher Dispatcher { get; }
    }
}
