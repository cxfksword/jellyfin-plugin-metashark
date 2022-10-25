using System;
using System.Threading;
using System.Threading.Tasks;
using TMDbLib.Objects.General;
using TMDbLib.Objects.Search;
using TMDbLib.Rest;

namespace TMDbLib.Client
{
    public partial class TMDbClient
    {
        private async Task<T> SearchMethodInternal<T>(string method, string query, int page, string language = null, bool? includeAdult = null, int year = 0, string dateFormat = null, string region = null, int primaryReleaseYear = 0, int firstAirDateYear = 0, CancellationToken cancellationToken = default) where T : new()
        {
            RestRequest req = _client.Create("search/{method}");
            req.AddUrlSegment("method", method);
            req.AddParameter("query", query);

            language ??= DefaultLanguage;
            if (!string.IsNullOrWhiteSpace(language))
                req.AddParameter("language", language);

            if (page >= 1)
                req.AddParameter("page", page.ToString());
            if (year >= 1)
                req.AddParameter("year", year.ToString());
            if (includeAdult.HasValue)
                req.AddParameter("include_adult", includeAdult.Value ? "true" : "false");

            // TODO: Dateformat?
            //if (dateFormat != null)
            //    req.DateFormat = dateFormat;

            if (!string.IsNullOrWhiteSpace(region))
                req.AddParameter("region", region);

            if (primaryReleaseYear >= 1)
                req.AddParameter("primary_release_year", primaryReleaseYear.ToString());
            if (firstAirDateYear >= 1)
                req.AddParameter("first_air_date_year", firstAirDateYear.ToString());

            T resp = await req.GetOfT<T>(cancellationToken).ConfigureAwait(false);

            return resp;
        }

        public async Task<SearchContainer<SearchCollection>> SearchCollectionAsync(string query, int page = 0, CancellationToken cancellationToken = default)
        {
            return await SearchCollectionAsync(query, DefaultLanguage, page, cancellationToken).ConfigureAwait(false);
        }

        public async Task<SearchContainer<SearchCollection>> SearchCollectionAsync(string query, string language, int page = 0, CancellationToken cancellationToken = default)
        {
            return await SearchMethodInternal<SearchContainer<SearchCollection>>("collection", query, page, language, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        public async Task<SearchContainer<SearchCompany>> SearchCompanyAsync(string query, int page = 0, CancellationToken cancellationToken = default)
        {
            return await SearchMethodInternal<SearchContainer<SearchCompany>>("company", query, page, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        public async Task<SearchContainer<SearchKeyword>> SearchKeywordAsync(string query, int page = 0, CancellationToken cancellationToken = default)
        {
            return await SearchMethodInternal<SearchContainer<SearchKeyword>>("keyword", query, page, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        [Obsolete("20200701 No longer present in public API")]
        public async Task<SearchContainer<SearchList>> SearchListAsync(string query, int page = 0, bool includeAdult = false, CancellationToken cancellationToken = default)
        {
            return await SearchMethodInternal<SearchContainer<SearchList>>("list", query, page, includeAdult: includeAdult, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        public async Task<SearchContainer<SearchMovie>> SearchMovieAsync(string query, int page = 0, bool includeAdult = false, int year = 0, string region = null, int primaryReleaseYear = 0, CancellationToken cancellationToken = default)
        {
            return await SearchMovieAsync(query, DefaultLanguage, page, includeAdult, year, region, primaryReleaseYear, cancellationToken).ConfigureAwait(false);
        }

        public async Task<SearchContainer<SearchMovie>> SearchMovieAsync(string query, string language, int page = 0, bool includeAdult = false, int year = 0, string region = null, int primaryReleaseYear = 0, CancellationToken cancellationToken = default)
        {
            return await SearchMethodInternal<SearchContainer<SearchMovie>>("movie", query, page, language, includeAdult, year, "yyyy-MM-dd", region, primaryReleaseYear, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        public async Task<SearchContainer<SearchBase>> SearchMultiAsync(string query, int page = 0, bool includeAdult = false, int year = 0, string region = null, CancellationToken cancellationToken = default)
        {
            return await SearchMultiAsync(query, DefaultLanguage, page, includeAdult, year, region, cancellationToken).ConfigureAwait(false);
        }

        public async Task<SearchContainer<SearchBase>> SearchMultiAsync(string query, string language, int page = 0, bool includeAdult = false, int year = 0, string region = null, CancellationToken cancellationToken = default)
        {
            return await SearchMethodInternal<SearchContainer<SearchBase>>("multi", query, page, language, includeAdult, year, "yyyy-MM-dd", region, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        public async Task<SearchContainer<SearchPerson>> SearchPersonAsync(string query, int page = 0, bool includeAdult = false, string region = null, CancellationToken cancellationToken = default)
        {
            return await SearchPersonAsync(query, DefaultLanguage, page, includeAdult, region, cancellationToken).ConfigureAwait(false);
        }

        public async Task<SearchContainer<SearchPerson>> SearchPersonAsync(string query, string language, int page = 0, bool includeAdult = false, string region = null, CancellationToken cancellationToken = default)
        {
            return await SearchMethodInternal<SearchContainer<SearchPerson>>("person", query, page, language, includeAdult, region: region, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        public async Task<SearchContainer<SearchTv>> SearchTvShowAsync(string query, int page = 0, bool includeAdult = false, int firstAirDateYear = 0, CancellationToken cancellationToken = default)
        {
            return await SearchTvShowAsync(query, DefaultLanguage, page, includeAdult, firstAirDateYear, cancellationToken).ConfigureAwait(false);
        }

        public async Task<SearchContainer<SearchTv>> SearchTvShowAsync(string query, string language, int page = 0, bool includeAdult = false, int firstAirDateYear = 0, CancellationToken cancellationToken = default)
        {
            return await SearchMethodInternal<SearchContainer<SearchTv>>("tv", query, page, language, includeAdult, firstAirDateYear: firstAirDateYear, cancellationToken: cancellationToken).ConfigureAwait(false);
        }
    }
}