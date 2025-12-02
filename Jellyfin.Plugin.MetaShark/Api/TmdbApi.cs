using Jellyfin.Plugin.MetaShark.Configuration;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TMDbLib.Client;
using TMDbLib.Objects.Collections;
using TMDbLib.Objects.Find;
using TMDbLib.Objects.General;
using TMDbLib.Objects.Movies;
using TMDbLib.Objects.People;
using TMDbLib.Objects.Search;
using TMDbLib.Objects.TvShows;

namespace Jellyfin.Plugin.MetaShark.Api
{
    public class TmdbApi : IDisposable
    {
        public const string DEFAULT_API_KEY = "4219e299c89411838049ab0dab19ebd5";
        public const string DEFAULT_API_HOST = "api.tmdb.org";
        private const int CacheDurationInHours = 1;
        private readonly ILogger<TmdbApi> _logger;
        private readonly IMemoryCache _memoryCache;
        private readonly TMDbClient _tmDbClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="TmdbApi"/> class.
        /// </summary>
        public TmdbApi(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<TmdbApi>();
            _memoryCache = new MemoryCache(new MemoryCacheOptions());
            var config = Plugin.Instance?.Configuration;
            var apiKey = string.IsNullOrEmpty(config?.TmdbApiKey) ? DEFAULT_API_KEY : config.TmdbApiKey;
            var host = string.IsNullOrEmpty(config?.TmdbHost) ? DEFAULT_API_HOST : config.TmdbHost;
            _tmDbClient = new TMDbClient(apiKey, true, host, null, config?.GetTmdbWebProxy());
            _tmDbClient.Timeout = TimeSpan.FromSeconds(10);
            // Not really interested in NotFoundException
            _tmDbClient.ThrowApiExceptions = false;
        }

        /// <summary>
        /// Gets a movie from the TMDb API based on its TMDb id.
        /// </summary>
        /// <param name="tmdbId">The movie's TMDb id.</param>
        /// <param name="language">The movie's language.</param>
        /// <param name="imageLanguages">A comma-separated list of image languages.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The TMDb movie or null if not found.</returns>
        public async Task<Movie?> GetMovieAsync(int tmdbId, string language, string imageLanguages, CancellationToken cancellationToken)
        {
            if (!this.IsEnable())
            {
                return null;
            }

            var key = $"movie-{tmdbId.ToString(CultureInfo.InvariantCulture)}-{language}-{imageLanguages}";
            if (_memoryCache.TryGetValue(key, out Movie movie))
            {
                return movie;
            }

            try
            {
                await EnsureClientConfigAsync().ConfigureAwait(false);

                movie = await _tmDbClient.GetMovieAsync(
                    tmdbId,
                    NormalizeLanguage(language),
                    GetImageLanguagesParam(imageLanguages),
                    MovieMethods.Credits | MovieMethods.Releases | MovieMethods.Images | MovieMethods.Keywords | MovieMethods.Videos,
                    cancellationToken).ConfigureAwait(false);

                if (movie != null)
                {
                    _memoryCache.Set(key, movie, TimeSpan.FromHours(CacheDurationInHours));
                }

                return movie;
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Gets a movie images from the TMDb API based on its TMDb id.
        /// </summary>
        /// <param name="tmdbId">The movie's TMDb id.</param>
        /// <param name="language">The movie's language.</param>
        /// <param name="imageLanguages">A comma-separated list of image languages.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The TMDb movie images or null if not found.</returns>
        public async Task<ImagesWithId?> GetMovieImagesAsync(int tmdbId, string language, string imageLanguages, CancellationToken cancellationToken)
        {
            if (!this.IsEnable())
            {
                return null;
            }

            var key = $"movie-images-{tmdbId.ToString(CultureInfo.InvariantCulture)}-{language}-{imageLanguages}";
            if (_memoryCache.TryGetValue(key, out ImagesWithId images))
            {
                return images;
            }

            try
            {
                await EnsureClientConfigAsync().ConfigureAwait(false);

                images = await _tmDbClient.GetMovieImagesAsync(
                    tmdbId,
                    NormalizeLanguage(language),
                    GetImageLanguagesParam(imageLanguages),
                    cancellationToken).ConfigureAwait(false);

                if (images != null)
                {
                    _memoryCache.Set(key, images, TimeSpan.FromHours(CacheDurationInHours));
                }

                return images;
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Gets a collection from the TMDb API based on its TMDb id.
        /// </summary>
        /// <param name="tmdbId">The collection's TMDb id.</param>
        /// <param name="language">The collection's language.</param>
        /// <param name="imageLanguages">A comma-separated list of image languages.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The TMDb collection or null if not found.</returns>
        public async Task<Collection?> GetCollectionAsync(int tmdbId, string language, string imageLanguages, CancellationToken cancellationToken)
        {
            var key = $"collection-{tmdbId.ToString(CultureInfo.InvariantCulture)}-{language}-{imageLanguages}";
            if (_memoryCache.TryGetValue(key, out Collection collection))
            {
                return collection;
            }

            await EnsureClientConfigAsync().ConfigureAwait(false);

            collection = await _tmDbClient.GetCollectionAsync(
                tmdbId,
                NormalizeLanguage(language),
                GetImageLanguagesParam(imageLanguages),
                CollectionMethods.Images,
                cancellationToken).ConfigureAwait(false);

            if (collection != null)
            {
                _memoryCache.Set(key, collection, TimeSpan.FromHours(CacheDurationInHours));
            }

            return collection;
        }

        /// <summary>
        /// Gets a tv show from the TMDb API based on its TMDb id.
        /// </summary>
        /// <param name="tmdbId">The tv show's TMDb id.</param>
        /// <param name="language">The tv show's language.</param>
        /// <param name="imageLanguages">A comma-separated list of image languages.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The TMDb tv show information or null if not found.</returns>
        public async Task<TvShow?> GetSeriesAsync(int tmdbId, string language, string imageLanguages, CancellationToken cancellationToken)
        {
            if (!this.IsEnable())
            {
                return null;
            }

            var key = $"series-{tmdbId.ToString(CultureInfo.InvariantCulture)}-{language}-{imageLanguages}";
            if (_memoryCache.TryGetValue(key, out TvShow series))
            {
                return series;
            }

            try
            {
                await EnsureClientConfigAsync().ConfigureAwait(false);

                series = await _tmDbClient.GetTvShowAsync(
                    tmdbId,
                    language: NormalizeLanguage(language),
                    includeImageLanguage: GetImageLanguagesParam(imageLanguages),
                    extraMethods: TvShowMethods.Credits | TvShowMethods.Images | TvShowMethods.Keywords | TvShowMethods.ExternalIds | TvShowMethods.Videos | TvShowMethods.ContentRatings | TvShowMethods.EpisodeGroups,
                    cancellationToken: cancellationToken).ConfigureAwait(false);

                if (series != null)
                {
                    _memoryCache.Set(key, series, TimeSpan.FromHours(CacheDurationInHours));
                }

                return series;
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Gets a tv show images from the TMDb API based on its TMDb id.
        /// </summary>
        /// <param name="tmdbId">The tv show's TMDb id.</param>
        /// <param name="language">The tv show's language.</param>
        /// <param name="imageLanguages">A comma-separated list of image languages.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The TMDb tv show images or null if not found.</returns>
        public async Task<ImagesWithId?> GetSeriesImagesAsync(int tmdbId, string language, string imageLanguages, CancellationToken cancellationToken)
        {
            if (!this.IsEnable())
            {
                return null;
            }

            var key = $"series-images-{tmdbId.ToString(CultureInfo.InvariantCulture)}-{language}-{imageLanguages}";
            if (_memoryCache.TryGetValue(key, out ImagesWithId images))
            {
                return images;
            }

            try
            {
                await EnsureClientConfigAsync().ConfigureAwait(false);

                images = await _tmDbClient.GetTvShowImagesAsync(
                    tmdbId,
                    NormalizeLanguage(language),
                    GetImageLanguagesParam(imageLanguages),
                    cancellationToken).ConfigureAwait(false);

                if (images != null)
                {
                    _memoryCache.Set(key, images, TimeSpan.FromHours(CacheDurationInHours));
                }

                return images;
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, ex.Message);
                return null;
            }
        }

        public async Task<TvGroupCollection?> GetSeriesGroupAsync(int tvShowId, string displayOrder, string? language, string? imageLanguages, CancellationToken cancellationToken)
        {
            if (!this.IsEnable())
            {
                return null;
            }

            TvGroupType? groupType =
                string.Equals(displayOrder, "originalAirDate", StringComparison.Ordinal) ? TvGroupType.OriginalAirDate :
                string.Equals(displayOrder, "absolute", StringComparison.Ordinal) ? TvGroupType.Absolute :
                string.Equals(displayOrder, "dvd", StringComparison.Ordinal) ? TvGroupType.DVD :
                string.Equals(displayOrder, "digital", StringComparison.Ordinal) ? TvGroupType.Digital :
                string.Equals(displayOrder, "storyArc", StringComparison.Ordinal) ? TvGroupType.StoryArc :
                string.Equals(displayOrder, "production", StringComparison.Ordinal) ? TvGroupType.Production :
                string.Equals(displayOrder, "tv", StringComparison.Ordinal) ? TvGroupType.TV :
                null;

            if (groupType is null)
            {
                return null;
            }

            var key = $"group-{tvShowId.ToString(CultureInfo.InvariantCulture)}-{displayOrder}-{language}";
            if (_memoryCache.TryGetValue(key, out TvGroupCollection? group))
            {
                return group;
            }

            try
            {
                await EnsureClientConfigAsync().ConfigureAwait(false);

                var series = await GetSeriesAsync(tvShowId, language, imageLanguages, cancellationToken).ConfigureAwait(false);
                var episodeGroupId = series?.EpisodeGroups.Results.Find(g => g.Type == groupType)?.Id;

                if (episodeGroupId is null)
                {
                    return null;
                }

                group = await _tmDbClient.GetTvEpisodeGroupsAsync(
                    episodeGroupId,
                    language: NormalizeLanguage(language),
                    cancellationToken: cancellationToken).ConfigureAwait(false);

                if (group is not null)
                {
                    _memoryCache.Set(key, group, TimeSpan.FromHours(CacheDurationInHours));
                }

                return group;
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Gets a tv season from the TMDb API based on the tv show's TMDb id.
        /// </summary>
        /// <param name="tvShowId">The tv season's TMDb id.</param>
        /// <param name="seasonNumber">The season number.</param>
        /// <param name="language">The tv season's language.</param>
        /// <param name="imageLanguages">A comma-separated list of image languages.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The TMDb tv season information or null if not found.</returns>
        public async Task<TvSeason?> GetSeasonAsync(int tvShowId, int seasonNumber, string language, string imageLanguages, CancellationToken cancellationToken)
        {
            if (!this.IsEnable())
            {
                return null;
            }

            var key = $"season-{tvShowId.ToString(CultureInfo.InvariantCulture)}-s{seasonNumber.ToString(CultureInfo.InvariantCulture)}-{language}-{imageLanguages}";
            if (_memoryCache.TryGetValue(key, out TvSeason season))
            {
                return season;
            }

            try
            {
                await EnsureClientConfigAsync().ConfigureAwait(false);

                season = await _tmDbClient.GetTvSeasonAsync(
                    tvShowId,
                    seasonNumber,
                    language: NormalizeLanguage(language),
                    includeImageLanguage: GetImageLanguagesParam(imageLanguages),
                    extraMethods: TvSeasonMethods.Credits | TvSeasonMethods.Images | TvSeasonMethods.ExternalIds | TvSeasonMethods.Videos,
                    cancellationToken: cancellationToken).ConfigureAwait(false);

                _memoryCache.Set(key, season, TimeSpan.FromHours(CacheDurationInHours));
                return season;
            }
            catch (Exception ex)
            {
                // 可能网络有问题，缓存一下避免频繁请求
                _memoryCache.Set(key, season, TimeSpan.FromSeconds(30));
                this._logger.LogError(ex, ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Gets a movie from the TMDb API based on the tv show's TMDb id.
        /// </summary>
        /// <param name="tvShowId">The tv show's TMDb id.</param>
        /// <param name="seasonNumber">The season number.</param>
        /// <param name="episodeNumber">The episode number.</param>
        /// <param name="language">The episode's language.</param>
        /// <param name="imageLanguages">A comma-separated list of image languages.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The TMDb tv episode information or null if not found.</returns>
        public async Task<TvEpisode?> GetEpisodeAsync(int tvShowId, int seasonNumber, int episodeNumber, string language, string imageLanguages, CancellationToken cancellationToken)
        {
            if (!this.IsEnable())
            {
                return null;
            }

            var key = $"episode-{tvShowId.ToString(CultureInfo.InvariantCulture)}-s{seasonNumber.ToString(CultureInfo.InvariantCulture)}e{episodeNumber.ToString(CultureInfo.InvariantCulture)}-{language}-{imageLanguages}";
            if (_memoryCache.TryGetValue(key, out TvEpisode episode))
            {
                return episode;
            }

            try
            {
                await EnsureClientConfigAsync().ConfigureAwait(false);

                episode = await _tmDbClient.GetTvEpisodeAsync(
                    tvShowId,
                    seasonNumber,
                    episodeNumber,
                    language: NormalizeLanguage(language),
                    includeImageLanguage: GetImageLanguagesParam(imageLanguages),
                    extraMethods: TvEpisodeMethods.Credits | TvEpisodeMethods.Images | TvEpisodeMethods.ExternalIds | TvEpisodeMethods.Videos,
                    cancellationToken: cancellationToken).ConfigureAwait(false);

                if (episode != null)
                {
                    _memoryCache.Set(key, episode, TimeSpan.FromHours(CacheDurationInHours));
                }

                return episode;
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Gets a person eg. cast or crew member from the TMDb API based on its TMDb id.
        /// </summary>
        /// <param name="personTmdbId">The person's TMDb id.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The TMDb person information or null if not found.</returns>
        public async Task<Person?> GetPersonAsync(int personTmdbId, CancellationToken cancellationToken)
        {
            if (!this.IsEnable())
            {
                return null;
            }

            var key = $"person-{personTmdbId.ToString(CultureInfo.InvariantCulture)}";
            if (_memoryCache.TryGetValue(key, out Person person))
            {
                return person;
            }

            try
            {

                await EnsureClientConfigAsync().ConfigureAwait(false);

                person = await _tmDbClient.GetPersonAsync(
                    personTmdbId,
                    PersonMethods.TvCredits | PersonMethods.MovieCredits | PersonMethods.Images | PersonMethods.ExternalIds,
                    cancellationToken).ConfigureAwait(false);

                if (person != null)
                {
                    _memoryCache.Set(key, person, TimeSpan.FromHours(CacheDurationInHours));
                }

                return person;
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Gets an item from the TMDb API based on its id from an external service eg. IMDb id, TvDb id.
        /// </summary>
        /// <param name="externalId">The item's external id.</param>
        /// <param name="source">The source of the id eg. IMDb.</param>
        /// <param name="language">The item's language.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The TMDb item or null if not found.</returns>
        public async Task<FindContainer?> FindByExternalIdAsync(
            string externalId,
            FindExternalSource source,
            string language,
            CancellationToken cancellationToken)
        {
            if (!this.IsEnable())
            {
                return null;
            }

            var key = $"find-{source.ToString()}-{externalId.ToString(CultureInfo.InvariantCulture)}-{language}";
            if (_memoryCache.TryGetValue(key, out FindContainer result))
            {
                return result;
            }

            try
            {
                await EnsureClientConfigAsync().ConfigureAwait(false);

                result = await _tmDbClient.FindAsync(
                    source,
                    externalId,
                    NormalizeLanguage(language),
                    cancellationToken).ConfigureAwait(false);

                if (result != null)
                {
                    _memoryCache.Set(key, result, TimeSpan.FromHours(CacheDurationInHours));
                }

                return result;
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Searches for a tv show using the TMDb API based on its name.
        /// </summary>
        /// <param name="name">The name of the tv show.</param>
        /// <param name="language">The tv show's language.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The TMDb tv show information.</returns>
        public async Task<IReadOnlyList<SearchTv>> SearchSeriesAsync(string name, string language, CancellationToken cancellationToken)
        {
            if (!this.IsEnable())
            {
                return new List<SearchTv>();
            }

            var key = $"searchseries-{name}-{language}";
            if (_memoryCache.TryGetValue(key, out SearchContainer<SearchTv> series))
            {
                return series.Results;
            }

            try
            {
                await EnsureClientConfigAsync().ConfigureAwait(false);

                var searchResults = await _tmDbClient
                    .SearchTvShowAsync(name, NormalizeLanguage(language), cancellationToken: cancellationToken)
                    .ConfigureAwait(false);

                if (searchResults.Results.Count > 0)
                {
                    _memoryCache.Set(key, searchResults, TimeSpan.FromHours(CacheDurationInHours));
                }

                return searchResults.Results;
            }
            catch (Exception ex)
            {
                return new List<SearchTv>();
            }
        }

        /// <summary>
        /// Searches for a person based on their name using the TMDb API.
        /// </summary>
        /// <param name="name">The name of the person.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The TMDb person information.</returns>
        public async Task<IReadOnlyList<SearchPerson>> SearchPersonAsync(string name, CancellationToken cancellationToken)
        {
            if (!this.IsEnable())
            {
                return new List<SearchPerson>();
            }

            var key = $"searchperson-{name}";
            if (_memoryCache.TryGetValue(key, out SearchContainer<SearchPerson> person))
            {
                return person.Results;
            }

            try
            {
                await EnsureClientConfigAsync().ConfigureAwait(false);

                var searchResults = await _tmDbClient
                    .SearchPersonAsync(name, cancellationToken: cancellationToken)
                    .ConfigureAwait(false);

                if (searchResults.Results.Count > 0)
                {
                    _memoryCache.Set(key, searchResults, TimeSpan.FromHours(CacheDurationInHours));
                }

                return searchResults.Results;
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, ex.Message);
                return new List<SearchPerson>();
            }
        }

        /// <summary>
        /// Searches for a movie based on its name using the TMDb API.
        /// </summary>
        /// <param name="name">The name of the movie.</param>
        /// <param name="language">The movie's language.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The TMDb movie information.</returns>
        public Task<IReadOnlyList<SearchMovie>> SearchMovieAsync(string name, string language, CancellationToken cancellationToken)
        {
            return SearchMovieAsync(name, 0, language, cancellationToken);
        }

        /// <summary>
        /// Searches for a movie based on its name using the TMDb API.
        /// </summary>
        /// <param name="name">The name of the movie.</param>
        /// <param name="year">The release year of the movie.</param>
        /// <param name="language">The movie's language.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The TMDb movie information.</returns>
        public async Task<IReadOnlyList<SearchMovie>> SearchMovieAsync(string name, int year, string language, CancellationToken cancellationToken)
        {
            if (!this.IsEnable())
            {
                return new List<SearchMovie>();
            }

            var key = $"moviesearch-{name}-{year.ToString(CultureInfo.InvariantCulture)}-{language}";
            if (_memoryCache.TryGetValue(key, out SearchContainer<SearchMovie> movies))
            {
                return movies.Results;
            }

            try
            {
                await EnsureClientConfigAsync().ConfigureAwait(false);

                var searchResults = await _tmDbClient
                    .SearchMovieAsync(name, NormalizeLanguage(language), year: year, cancellationToken: cancellationToken)
                    .ConfigureAwait(false);

                if (searchResults.Results.Count > 0)
                {
                    _memoryCache.Set(key, searchResults, TimeSpan.FromHours(CacheDurationInHours));
                }

                return searchResults.Results;
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, ex.Message);
                return new List<SearchMovie>();
            }
        }

        /// <summary>
        /// Searches for a collection based on its name using the TMDb API.
        /// </summary>
        /// <param name="name">The name of the collection.</param>
        /// <param name="language">The collection's language.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The TMDb collection information.</returns>
        public async Task<IReadOnlyList<SearchCollection>> SearchCollectionAsync(string name, string language, CancellationToken cancellationToken)
        {
            if (!this.IsEnable())
            {
                return new List<SearchCollection>();
            }

            var key = $"collectionsearch-{name}-{language}";
            if (_memoryCache.TryGetValue(key, out SearchContainer<SearchCollection> collections))
            {
                return collections.Results;
            }

            try
            {
                await EnsureClientConfigAsync().ConfigureAwait(false);

                var searchResults = await _tmDbClient
                    .SearchCollectionAsync(name, NormalizeLanguage(language), cancellationToken: cancellationToken)
                    .ConfigureAwait(false);

                if (searchResults.Results.Count > 0)
                {
                    _memoryCache.Set(key, searchResults, TimeSpan.FromHours(CacheDurationInHours));
                }

                return searchResults.Results;
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, ex.Message);
                return new List<SearchCollection>();
            }
        }

        /// <summary>
        /// Gets the absolute URL of the poster.
        /// </summary>
        /// <param name="posterPath">The relative URL of the poster.</param>
        /// <returns>The absolute URL.</returns>
        public string? GetPosterUrl(string posterPath)
        {
            if (string.IsNullOrEmpty(posterPath))
            {
                return null;
            }

            return _tmDbClient.GetImageUrl(_tmDbClient.Config.Images.PosterSizes[^1], posterPath, true).ToString();
        }

        /// <summary>
        /// Gets the absolute URL of the backdrop image.
        /// </summary>
        /// <param name="posterPath">The relative URL of the backdrop image.</param>
        /// <returns>The absolute URL.</returns>
        public string? GetBackdropUrl(string posterPath)
        {
            if (string.IsNullOrEmpty(posterPath))
            {
                return null;
            }

            return _tmDbClient.GetImageUrl(_tmDbClient.Config.Images.BackdropSizes[^1], posterPath, true).ToString();
        }

        /// <summary>
        /// Gets the absolute URL of the profile image.
        /// </summary>
        /// <param name="actorProfilePath">The relative URL of the profile image.</param>
        /// <returns>The absolute URL.</returns>
        public string? GetProfileUrl(string actorProfilePath)
        {
            if (string.IsNullOrEmpty(actorProfilePath))
            {
                return null;
            }

            return _tmDbClient.GetImageUrl(_tmDbClient.Config.Images.ProfileSizes[^1], actorProfilePath, true).ToString();
        }

        /// <summary>
        /// Gets the absolute URL of the still image.
        /// </summary>
        /// <param name="filePath">The relative URL of the still image.</param>
        /// <returns>The absolute URL.</returns>
        public string? GetStillUrl(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                return null;
            }

            return _tmDbClient.GetImageUrl(_tmDbClient.Config.Images.StillSizes[^1], filePath, true).ToString();
        }

        public string? GetLogoUrl(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                return null;
            }

            return _tmDbClient.GetImageUrl(_tmDbClient.Config.Images.LogoSizes[^1], filePath, true).ToString();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose.
        /// </summary>
        /// <param name="disposing">Dispose all members.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _memoryCache.Dispose();
                _tmDbClient.Dispose();
            }
        }

        private Task EnsureClientConfigAsync()
        {
            return !_tmDbClient.HasConfig ? _tmDbClient.GetConfigAsync() : Task.CompletedTask;
        }

        /// <summary>
        /// Normalizes a language string for use with TMDb's language parameter.
        /// </summary>
        /// <param name="language">The language code.</param>
        /// <returns>The normalized language code.</returns>
        public string NormalizeLanguage(string language)
        {
            if (string.IsNullOrEmpty(language))
            {
                return language;
            }

            // They require this to be uppercase
            // Everything after the hyphen must be written in uppercase due to a way TMDB wrote their api.
            // See here: https://www.themoviedb.org/talk/5119221d760ee36c642af4ad?page=3#56e372a0c3a3685a9e0019ab
            var parts = language.Split('-');

            if (parts.Length == 2)
            {
                language = parts[0] + "-" + parts[1].ToUpperInvariant();
            }

            return language;
        }


        public string GetImageLanguagesParam(string preferredLanguage)
        {
            if (string.IsNullOrEmpty(preferredLanguage))
            {
                return null;
            }

            var languages = new List<string>();

            if (!string.IsNullOrEmpty(preferredLanguage))
            {
                var parts = preferredLanguage.Split(',');
                foreach (var lang in parts)
                {
                    var l = this.NormalizeLanguage(lang);
                    if (l.Length == 5) // like en-US
                    {
                        // Currently, TMDB supports 2-letter language codes only
                        // They are planning to change this in the future, thus we're
                        // supplying both codes if we're having a 5-letter code.
                        languages.Add(l.Substring(0, 2));
                    }
                    else
                    {
                        languages.Add(l);
                    }
                }
            }

            languages.Add("null");

            if (!languages.Contains("en"))
            {
                languages.Add("en");
            }

            return string.Join(',', languages);
        }

        private bool IsEnable()
        {
            return Plugin.Instance?.Configuration.EnableTmdb ?? true;
        }

    }
}
