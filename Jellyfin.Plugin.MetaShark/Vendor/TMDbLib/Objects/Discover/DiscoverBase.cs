using System.Threading;
using System.Threading.Tasks;
using TMDbLib.Client;
using TMDbLib.Objects.General;
using TMDbLib.Utilities;

namespace TMDbLib.Objects.Discover
{
    public abstract class DiscoverBase<T>
    {
        private readonly TMDbClient _client;
        private readonly string _endpoint;
        protected readonly SimpleNamedValueCollection Parameters;

        public DiscoverBase(string endpoint, TMDbClient client)
        {
            _endpoint = endpoint;
            _client = client;
            Parameters = new SimpleNamedValueCollection();
        }

        public async Task<SearchContainer<T>> Query(int page = 0, CancellationToken cancellationToken = default)
        {
            return await Query(_client.DefaultLanguage, page, cancellationToken).ConfigureAwait(false);
        }

        public async Task<SearchContainer<T>> Query(string language, int page = 0, CancellationToken cancellationToken = default)
        {
            return await _client.DiscoverPerformAsync<T>(_endpoint, language, page, Parameters, cancellationToken).ConfigureAwait(false);
        }
    }
}