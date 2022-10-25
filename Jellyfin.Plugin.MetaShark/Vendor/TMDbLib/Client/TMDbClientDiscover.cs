using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TMDbLib.Objects.Discover;
using TMDbLib.Objects.General;
using TMDbLib.Rest;
using TMDbLib.Utilities;

namespace TMDbLib.Client
{
    public partial class TMDbClient
    {
        /// <summary>
        /// Can be used to discover movies matching certain criteria
        /// </summary>
        public DiscoverMovie DiscoverMoviesAsync()
        {
            return new DiscoverMovie(this);
        }

        internal async Task<SearchContainer<T>> DiscoverPerformAsync<T>(string endpoint, string language, int page, SimpleNamedValueCollection parameters, CancellationToken cancellationToken = default)
        {
            RestRequest request = _client.Create(endpoint);

            if (page != 1 && page > 1)
                request.AddParameter("page", page.ToString());

            if (!string.IsNullOrWhiteSpace(language))
                request.AddParameter("language", language);

            foreach (KeyValuePair<string, string> pair in parameters)
                request.AddParameter(pair.Key, pair.Value);

            SearchContainer<T> response = await request.GetOfT<SearchContainer<T>>(cancellationToken).ConfigureAwait(false);
            return response;
        }

        /// <summary>
        /// Can be used to discover new tv shows matching certain criteria
        /// </summary>
        public DiscoverTv DiscoverTvShowsAsync()
        {
            return new DiscoverTv(this);
        }
    }
}
