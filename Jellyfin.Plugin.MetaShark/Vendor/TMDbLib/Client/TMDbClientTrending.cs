using System.Threading;
using System.Threading.Tasks;
using TMDbLib.Objects.General;
using TMDbLib.Objects.Search;
using TMDbLib.Objects.Trending;
using TMDbLib.Rest;
using TMDbLib.Utilities;

namespace TMDbLib.Client
{
    public partial class TMDbClient
    {
        public async Task<SearchContainer<SearchMovie>> GetTrendingMoviesAsync(TimeWindow timeWindow, int page = 0, CancellationToken cancellationToken = default)
        {
            RestRequest req = _client.Create("trending/movie/{time_window}");
            req.AddUrlSegment("time_window", timeWindow.GetDescription());

            if (page >= 1)
                req.AddQueryString("page", page.ToString());

            SearchContainer<SearchMovie> resp = await req.GetOfT<SearchContainer<SearchMovie>>(cancellationToken).ConfigureAwait(false);

            return resp;
        }

        public async Task<SearchContainer<SearchTv>> GetTrendingTvAsync(TimeWindow timeWindow, int page = 0, CancellationToken cancellationToken = default)
        {
            RestRequest req = _client.Create("trending/tv/{time_window}");
            req.AddUrlSegment("time_window", timeWindow.GetDescription());

            if (page >= 1)
                req.AddQueryString("page", page.ToString());

            SearchContainer<SearchTv> resp = await req.GetOfT<SearchContainer<SearchTv>>(cancellationToken).ConfigureAwait(false);

            return resp;
        }

        public async Task<SearchContainer<SearchPerson>> GetTrendingPeopleAsync(TimeWindow timeWindow, int page = 0, CancellationToken cancellationToken = default)
        {
            RestRequest req = _client.Create("trending/person/{time_window}");
            req.AddUrlSegment("time_window", timeWindow.GetDescription());

            if (page >= 1)
                req.AddQueryString("page", page.ToString());

            SearchContainer<SearchPerson> resp = await req.GetOfT<SearchContainer<SearchPerson>>(cancellationToken).ConfigureAwait(false);

            return resp;
        }
    }
}
