namespace ComposableAsync
{
    /// <summary>
    /// <see cref="IDispatcherProvider"/> extension
    /// </summary>
    public static class DispatcherProviderExtension
    {
        /// <summary>
        /// Returns the underlying <see cref="IDispatcher"/>
        /// </summary>
        /// <param name="dispatcherProvider"></param>
        /// <returns></returns>
        public static IDispatcher GetAssociatedDispatcher(this IDispatcherProvider dispatcherProvider)
        {
            return dispatcherProvider?.Dispatcher ?? NullDispatcher.Instance;
        }
    }
}
