using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace ComposableAsync
{
    /// <summary>
    /// A <see cref="DelegatingHandler"/> implementation based on <see cref="IDispatcher"/>
    /// </summary>
    internal class DispatcherDelegatingHandler : DelegatingHandler
    {
        private readonly IDispatcher _Dispatcher;

        /// <summary>
        /// Build an <see cref="DelegatingHandler"/> from a <see cref="IDispatcher"/>
        /// </summary>
        /// <param name="dispatcher"></param>
        public DispatcherDelegatingHandler(IDispatcher dispatcher)
        {
            _Dispatcher = dispatcher;
            InnerHandler = new HttpClientHandler();
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return _Dispatcher.Enqueue(() => base.SendAsync(request, cancellationToken), cancellationToken);
        }
    }
}
