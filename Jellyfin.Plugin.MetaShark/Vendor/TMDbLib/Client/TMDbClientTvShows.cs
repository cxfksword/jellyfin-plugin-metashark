using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TMDbLib.Objects.Authentication;
using TMDbLib.Objects.Changes;
using TMDbLib.Objects.General;
using TMDbLib.Objects.Reviews;
using TMDbLib.Objects.Search;
using TMDbLib.Objects.TvShows;
using TMDbLib.Rest;
using TMDbLib.Utilities;
using Credits = TMDbLib.Objects.TvShows.Credits;

namespace TMDbLib.Client
{
    public partial class TMDbClient
    {
        private async Task<T> GetTvShowMethodInternal<T>(int id, TvShowMethods tvShowMethod, string dateFormat = null, string language = null, string includeImageLanguage = null, int page = 0, CancellationToken cancellationToken = default) where T : new()
        {
            RestRequest req = _client.Create("tv/{id}/{method}");
            req.AddUrlSegment("id", id.ToString(CultureInfo.InvariantCulture));
            req.AddUrlSegment("method", tvShowMethod.GetDescription());

            // TODO: Dateformat?
            //if (dateFormat != null)
            //    req.DateFormat = dateFormat;

            if (page > 0)
                req.AddParameter("page", page.ToString());

            language ??= DefaultLanguage;
            if (!string.IsNullOrWhiteSpace(language))
                req.AddParameter("language", language);

            includeImageLanguage ??= DefaultImageLanguage;
            if (!string.IsNullOrWhiteSpace(includeImageLanguage))
                req.AddParameter("include_image_language", includeImageLanguage);

            T resp = await req.GetOfT<T>(cancellationToken).ConfigureAwait(false);

            return resp;
        }

        private async Task<SearchContainer<SearchTv>> GetTvShowListInternal(int page, string language, string tvShowListType, CancellationToken cancellationToken = default)
        {
            RestRequest req = _client.Create("tv/" + tvShowListType);

            language ??= DefaultLanguage;
            if (!string.IsNullOrWhiteSpace(language))
                req.AddParameter("language", language);

            if (page >= 1)
                req.AddParameter("page", page.ToString());

            SearchContainer<SearchTv> response = await req.GetOfT<SearchContainer<SearchTv>>(cancellationToken).ConfigureAwait(false);

            return response;
        }

        public async Task<TvShow> GetLatestTvShowAsync(CancellationToken cancellationToken = default)
        {
            RestRequest req = _client.Create("tv/latest");

            TvShow resp = await req.GetOfT<TvShow>(cancellationToken).ConfigureAwait(false);

            return resp;
        }

        /// <summary>
        /// Retrieves all information for a specific tv show in relation to the current user account
        /// </summary>
        /// <param name="tvShowId">The id of the tv show to get the account states for</param>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <remarks>Requires a valid user session</remarks>
        /// <exception cref="UserSessionRequiredException">Thrown when the current client object doens't have a user session assigned.</exception>
        public async Task<AccountState> GetTvShowAccountStateAsync(int tvShowId, CancellationToken cancellationToken = default)
        {
            RequireSessionId(SessionType.UserSession);

            RestRequest req = _client.Create("tv/{tvShowId}/{method}");
            req.AddUrlSegment("tvShowId", tvShowId.ToString(CultureInfo.InvariantCulture));
            req.AddUrlSegment("method", TvShowMethods.AccountStates.GetDescription());
            AddSessionId(req, SessionType.UserSession);

            using RestResponse<AccountState> response = await req.Get<AccountState>(cancellationToken).ConfigureAwait(false);

            return await response.GetDataObject().ConfigureAwait(false);
        }

        public async Task<ResultContainer<AlternativeTitle>> GetTvShowAlternativeTitlesAsync(int id, CancellationToken cancellationToken = default)
        {
            return await GetTvShowMethodInternal<ResultContainer<AlternativeTitle>>(id, TvShowMethods.AlternativeTitles, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Retrieve a tv Show by id.
        /// </summary>
        /// <param name="id">TMDb id of the tv show to retrieve.</param>
        /// <param name="extraMethods">Enum flags indicating any additional data that should be fetched in the same request.</param>
        /// <param name="language">If specified the api will attempt to return a localized result. ex: en,it,es.</param>
        /// <param name="includeImageLanguage">If specified the api will attempt to return localized image results eg. en,it,es.</param>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <returns>The requested Tv Show</returns>
        public async Task<TvShow> GetTvShowAsync(int id, TvShowMethods extraMethods = TvShowMethods.Undefined, string language = null, string includeImageLanguage = null, CancellationToken cancellationToken = default)
        {
            if (extraMethods.HasFlag(TvShowMethods.AccountStates))
                RequireSessionId(SessionType.UserSession);

            RestRequest req = _client.Create("tv/{id}");
            req.AddUrlSegment("id", id.ToString(CultureInfo.InvariantCulture));

            if (extraMethods.HasFlag(TvShowMethods.AccountStates))
                AddSessionId(req, SessionType.UserSession);

            language ??= DefaultLanguage;
            if (!string.IsNullOrWhiteSpace(language))
                req.AddParameter("language", language);

            if (!string.IsNullOrWhiteSpace(includeImageLanguage))
                req.AddParameter("include_image_language", includeImageLanguage);

            string appends = string.Join(",",
                                         Enum.GetValues(typeof(TvShowMethods))
                                             .OfType<TvShowMethods>()
                                             .Except(new[] { TvShowMethods.Undefined })
                                             .Where(s => extraMethods.HasFlag(s))
                                             .Select(s => s.GetDescription()));

            if (appends != string.Empty)
                req.AddParameter("append_to_response", appends);

            using RestResponse<TvShow> response = await req.Get<TvShow>(cancellationToken).ConfigureAwait(false);

            if (!response.IsValid)
                return null;

            TvShow item = await response.GetDataObject().ConfigureAwait(false);

            // No data to patch up so return
            if (item == null)
                return null;

            // Patch up data, so that the end user won't notice that we share objects between request-types.
            if (item.Translations != null)
                item.Translations.Id = id;

            if (item.AccountStates != null)
                item.AccountStates.Id = id;

            if (item.Recommendations != null)
                item.Recommendations.Id = id;

            if (item.ExternalIds != null)
                item.ExternalIds.Id = id;

            return item;
        }

        public async Task<ChangesContainer> GetTvShowChangesAsync(int id, CancellationToken cancellationToken = default)
        {
            return await GetTvShowMethodInternal<ChangesContainer>(id, TvShowMethods.Changes, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        public async Task<ResultContainer<ContentRating>> GetTvShowContentRatingsAsync(int id, CancellationToken cancellationToken = default)
        {
            return await GetTvShowMethodInternal<ResultContainer<ContentRating>>(id, TvShowMethods.ContentRatings, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Returns a credits object for the tv show associated with the provided TMDb id.
        /// </summary>
        /// <param name="id">The TMDb id of the target tv show.</param>
        /// <param name="language">If specified the api will attempt to return a localized result. ex: en,it,es </param>
        /// <param name="cancellationToken">A cancellation token</param>
        public async Task<Credits> GetTvShowCreditsAsync(int id, string language = null, CancellationToken cancellationToken = default)
        {
            return await GetTvShowMethodInternal<Credits>(id, TvShowMethods.Credits, "yyyy-MM-dd", language, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Returns a credits object for the aggragation of tv show associated with the provided TMDb id.
        /// </summary>
        /// <param name="id">The TMDb id of the target tv show.</param>
        /// <param name="language">If specified the api will attempt to return a localized result. ex: en,it,es </param>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <returns></returns>
        public async Task<CreditsAggregate> GetAggregateCredits(int id, string language = null, CancellationToken cancellationToken = default)
        {
            return await GetTvShowMethodInternal<CreditsAggregate>(id, TvShowMethods.CreditsAggregate, language: language, page: 0, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Returns an object that contains all known exteral id's for the tv show related to the specified TMDB id.
        /// </summary>
        /// <param name="id">The TMDb id of the target tv show.</param>
        /// <param name="cancellationToken">A cancellation token</param>
        public async Task<ExternalIdsTvShow> GetTvShowExternalIdsAsync(int id, CancellationToken cancellationToken = default)
        {
            return await GetTvShowMethodInternal<ExternalIdsTvShow>(id, TvShowMethods.ExternalIds, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Retrieves all images all related to the specified tv show.
        /// </summary>
        /// <param name="id">The TMDb id of the target tv show.</param>
        /// <param name="language">
        /// If specified the api will attempt to return a localized result. ex: en,it,es.
        /// For images this means that the image might contain language specifc text
        /// </param>
        /// <param name="includeImageLanguage">If you want to include a fallback language (especially useful for backdrops) you can use the include_image_language parameter. This should be a comma separated value like so: include_image_language=en,null.</param>
        /// <param name="cancellationToken">A cancellation token</param>
        public async Task<ImagesWithId> GetTvShowImagesAsync(int id, string language = null, string includeImageLanguage = null, CancellationToken cancellationToken = default)
        {
            return await GetTvShowMethodInternal<ImagesWithId>(id, TvShowMethods.Images, language: language, includeImageLanguage: includeImageLanguage, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        public async Task<SearchContainerWithId<ReviewBase>> GetTvShowReviewsAsync(int id, string language = null, int page = 0, CancellationToken cancellationToken = default)
        {
            return await GetTvShowMethodInternal<SearchContainerWithId<ReviewBase>>(id, TvShowMethods.Reviews, language: language, page: page, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        public async Task<ResultContainer<Keyword>> GetTvShowKeywordsAsync(int id, CancellationToken cancellationToken = default)
        {
            return await GetTvShowMethodInternal<ResultContainer<Keyword>>(id, TvShowMethods.Keywords, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Fetches a dynamic list of TV Shows
        /// </summary>
        /// <param name="list">Type of list to fetch</param>
        /// <param name="page">Page</param>
        /// <param name="timezone">Only relevant for list type AiringToday</param>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <returns></returns>
        public async Task<SearchContainer<SearchTv>> GetTvShowListAsync(TvShowListType list, int page = 0, string timezone = null, CancellationToken cancellationToken = default)
        {
            return await GetTvShowListAsync(list, DefaultLanguage, page, timezone, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Fetches a dynamic list of TV Shows
        /// </summary>
        /// <param name="list">Type of list to fetch</param>
        /// <param name="language">Language</param>
        /// <param name="page">Page</param>
        /// <param name="timezone">Only relevant for list type AiringToday</param>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <returns></returns>
        public async Task<SearchContainer<SearchTv>> GetTvShowListAsync(TvShowListType list, string language, int page = 0, string timezone = null, CancellationToken cancellationToken = default)
        {
            RestRequest req = _client.Create("tv/{method}");
            req.AddUrlSegment("method", list.GetDescription());

            if (page > 0)
                req.AddParameter("page", page.ToString());

            if (!string.IsNullOrEmpty(timezone))
                req.AddParameter("timezone", timezone);

            language ??= DefaultLanguage;
            if (!string.IsNullOrWhiteSpace(language))
                req.AddParameter("language", language);

            SearchContainer<SearchTv> resp = await req.GetOfT<SearchContainer<SearchTv>>(cancellationToken).ConfigureAwait(false);

            return resp;
        }

        /// <summary>
        /// Get the list of popular TV shows. This list refreshes every day.
        /// </summary>
        /// <returns>
        /// Returns the basic information about a tv show.
        /// For additional data use the main GetTvShowAsync method using the tv show id as parameter.
        /// </returns>
        public async Task<SearchContainer<SearchTv>> GetTvShowPopularAsync(int page = -1, string language = null, CancellationToken cancellationToken = default)
        {
            return await GetTvShowListInternal(page, language, "popular", cancellationToken).ConfigureAwait(false);
        }

        public async Task<SearchContainer<SearchTv>> GetTvShowSimilarAsync(int id, int page = 0, CancellationToken cancellationToken = default)
        {
            return await GetTvShowSimilarAsync(id, DefaultLanguage, page, cancellationToken).ConfigureAwait(false);
        }

        public async Task<SearchContainer<SearchTv>> GetTvShowSimilarAsync(int id, string language, int page = 0, CancellationToken cancellationToken = default)
        {
            return await GetTvShowMethodInternal<SearchContainer<SearchTv>>(id, TvShowMethods.Similar, language: language, page: page, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        public async Task<SearchContainer<SearchTv>> GetTvShowRecommendationsAsync(int id, int page = 0, CancellationToken cancellationToken = default)
        {
            return await GetTvShowRecommendationsAsync(id, DefaultLanguage, page, cancellationToken).ConfigureAwait(false);
        }

        public async Task<SearchContainer<SearchTv>> GetTvShowRecommendationsAsync(int id, string language, int page = 0, CancellationToken cancellationToken = default)
        {
            return await GetTvShowMethodInternal<SearchContainer<SearchTv>>(id, TvShowMethods.Recommendations, language: language, page: page, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Get the list of top rated TV shows. By default, this list will only include TV shows that have 2 or more votes. This list refreshes every day.
        /// </summary>
        /// <returns>
        /// Returns the basic information about a tv show.
        /// For additional data use the main GetTvShowAsync method using the tv show id as parameter
        /// </returns>
        public async Task<SearchContainer<SearchTv>> GetTvShowTopRatedAsync(int page = -1, string language = null, CancellationToken cancellationToken = default)
        {
            return await GetTvShowListInternal(page, language, "top_rated", cancellationToken).ConfigureAwait(false);
        }

        public async Task<TranslationsContainerTv> GetTvShowTranslationsAsync(int id, CancellationToken cancellationToken = default)
        {
            return await GetTvShowMethodInternal<TranslationsContainerTv>(id, TvShowMethods.Translations, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        public async Task<ResultContainer<Video>> GetTvShowVideosAsync(int id, CancellationToken cancellationToken = default)
        {
            return await GetTvShowMethodInternal<ResultContainer<Video>>(id, TvShowMethods.Videos, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        public async Task<SingleResultContainer<Dictionary<string, WatchProviders>>> GetTvShowWatchProvidersAsync(int id, CancellationToken cancellationToken = default)
        {
            return await GetTvShowMethodInternal<SingleResultContainer<Dictionary<string, WatchProviders>>>(id, TvShowMethods.WatchProviders, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        public async Task<bool> TvShowRemoveRatingAsync(int tvShowId, CancellationToken cancellationToken = default)
        {
            RequireSessionId(SessionType.GuestSession);

            RestRequest req = _client.Create("tv/{tvShowId}/rating");
            req.AddUrlSegment("tvShowId", tvShowId.ToString(CultureInfo.InvariantCulture));
            AddSessionId(req);

            using RestResponse<PostReply> response = await req.Delete<PostReply>(cancellationToken).ConfigureAwait(false);

            // status code 13 = "The item/record was deleted successfully."
            PostReply item = await response.GetDataObject().ConfigureAwait(false);

            // TODO: Original code had a check for item=null
            return item.StatusCode == 13;
        }

        /// <summary>
        /// Change the rating of a specified tv show.
        /// </summary>
        /// <param name="tvShowId">The id of the tv show to rate</param>
        /// <param name="rating">The rating you wish to assign to the specified tv show. Value needs to be between 0.5 and 10 and must use increments of 0.5. Ex. using 7.1 will not work and return false.</param>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <returns>True if the the tv show's rating was successfully updated, false if not</returns>
        /// <remarks>Requires a valid guest or user session</remarks>
        /// <exception cref="GuestSessionRequiredException">Thrown when the current client object doens't have a guest or user session assigned.</exception>
        public async Task<bool> TvShowSetRatingAsync(int tvShowId, double rating, CancellationToken cancellationToken = default)
        {
            RequireSessionId(SessionType.GuestSession);

            RestRequest req = _client.Create("tv/{tvShowId}/rating");
            req.AddUrlSegment("tvShowId", tvShowId.ToString(CultureInfo.InvariantCulture));
            AddSessionId(req);

            req.SetBody(new { value = rating });

            using RestResponse<PostReply> response = await req.Post<PostReply>(cancellationToken).ConfigureAwait(false);

            // status code 1 = "Success"
            // status code 12 = "The item/record was updated successfully" - Used when an item was previously rated by the user
            PostReply item = await response.GetDataObject().ConfigureAwait(false);

            // TODO: Original code had a check for item=null
            return item.StatusCode == 1 || item.StatusCode == 12;
        }
    }
}