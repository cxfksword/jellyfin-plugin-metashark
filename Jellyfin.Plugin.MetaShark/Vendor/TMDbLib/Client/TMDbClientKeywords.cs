using System.Threading;
using System.Threading.Tasks;
using TMDbLib.Objects.General;
using TMDbLib.Objects.Search;
using TMDbLib.Rest;

namespace TMDbLib.Client
{
    public partial class TMDbClient
    {
        public async Task<Keyword> GetKeywordAsync(int keywordId, CancellationToken cancellationToken = default)
        {
            RestRequest req = _client.Create("keyword/{keywordId}");
            req.AddUrlSegment("keywordId", keywordId.ToString());

            Keyword resp = await req.GetOfT<Keyword>(cancellationToken).ConfigureAwait(false);

            return resp;
        }

        public async Task<SearchContainerWithId<SearchMovie>> GetKeywordMoviesAsync(int keywordId, int page = 0, CancellationToken cancellationToken = default)
        {
            return await GetKeywordMoviesAsync(keywordId, DefaultLanguage, page, cancellationToken).ConfigureAwait(false);
        }

        public async Task<SearchContainerWithId<SearchMovie>> GetKeywordMoviesAsync(int keywordId, string language, int page = 0, CancellationToken cancellationToken = default)
        {
            RestRequest req = _client.Create("keyword/{keywordId}/movies");
            req.AddUrlSegment("keywordId", keywordId.ToString());

            language ??= DefaultLanguage;
            if (!string.IsNullOrWhiteSpace(language))
                req.AddParameter("language", language);

            if (page >= 1)
                req.AddParameter("page", page.ToString());

            SearchContainerWithId<SearchMovie> resp = await req.GetOfT<SearchContainerWithId<SearchMovie>>(cancellationToken).ConfigureAwait(false);

            return resp;
        }
    }
}