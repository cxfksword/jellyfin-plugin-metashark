using Jellyfin.Plugin.MetaShark.Api;
using Jellyfin.Plugin.MetaShark.Model;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;
using StringMetric;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using TMDbLib.Objects.General;
using Jellyfin.Plugin.MetaShark.Configuration;
using Jellyfin.Plugin.MetaShark.Core;

namespace Jellyfin.Plugin.MetaShark.Providers
{
    public abstract class BaseProvider
    {
        /// <summary>
        /// Gets the provider name.
        /// </summary>
        public const string DoubanProviderName = "Douban";

        /// <summary>
        /// Gets the provider id.
        /// </summary>
        public const string DoubanProviderId = "DoubanID";

        /// <summary>
        /// Name of the provider.
        /// </summary>
        public const string TmdbProviderName = "TheMovieDb";

        protected readonly ILogger _logger;
        protected readonly IHttpClientFactory _httpClientFactory;
        protected readonly DoubanApi _doubanApi;
        protected readonly TmdbApi _tmdbApi;
        protected readonly OmdbApi _omdbApi;
        protected readonly ILibraryManager _libraryManager;

        protected Regex regMetaSourcePrefix = new Regex(@"^\[.+\]", RegexOptions.Compiled);

        public string Pattern
        {
            get
            {
                return this.config.Pattern;
            }
        }

        protected PluginConfiguration config
        {
            get
            {
                return Plugin.Instance?.Configuration ?? new PluginConfiguration();
            }
        }

        protected BaseProvider(IHttpClientFactory httpClientFactory, ILogger logger, ILibraryManager libraryManager, DoubanApi doubanApi, TmdbApi tmdbApi, OmdbApi omdbApi)
        {
            this._doubanApi = doubanApi;
            this._tmdbApi = tmdbApi;
            this._omdbApi = omdbApi;
            this._libraryManager = libraryManager;
            this._logger = logger;
            this._httpClientFactory = httpClientFactory;
        }

        protected async Task<string?> GuessByDoubanAsync(ItemLookupInfo info, CancellationToken cancellationToken)
        {
            // ParseName is required here.
            // Caller provides the filename with extension stripped and NOT the parsed filename
            var parsedName = this._libraryManager.ParseName(info.Name);
            this.Log($"GuessByDouban of [name]: {info.Name} year: {info.Year} search name: {parsedName.Name}");
            var result = await this._doubanApi.SearchAsync(parsedName.Name, cancellationToken).ConfigureAwait(false);
            var jw = new JaroWinkler();
            foreach (var item in result)
            {
                if (info is MovieInfo && item.Category != "电影")
                {
                    continue;
                }

                if (info is SeriesInfo && item.Category != "电视剧")
                {
                    continue;
                }

                if (jw.Similarity(parsedName.Name, item.Name) < 0.8)
                {
                    continue;
                }

                if (parsedName.Year == null || parsedName.Year == 0)
                {
                    this.Log($"GuessByDouban of [name] found Sid: {item.Sid}");
                    return item.Sid;
                }

                if (parsedName.Year == item.Year)
                {
                    this.Log($"GuessByDouban of [name] found Sid: {item.Sid}");
                    return item.Sid;
                }
            }

            return null;
        }

        protected async Task<string?> GuestDoubanSeasonByYearAsync(string name, int? year, CancellationToken cancellationToken)
        {
            if (year == null || year == 0)
            {
                return null;
            }

            this.Log($"GuestDoubanSeasonByYear of [name]: {name} year: {year}");
            var result = await this._doubanApi.SearchAsync(name, cancellationToken).ConfigureAwait(false);
            var jw = new JaroWinkler();
            foreach (var item in result)
            {
                if (item.Category != "电视剧")
                {
                    continue;
                }

                var score = jw.Similarity(name, item.Name);
                // this.Log($"GuestDoubanSeasonByYear name: {name} douban_name: {item.Name} douban_sid: {item.Sid} douban_year: {item.Year} score: {score} ");
                if (score < 0.8)
                {
                    continue;
                }

                if (year == item.Year)
                {
                    this.Log($"GuestDoubanSeasonByYear of [name] found Sid: {item.Sid}");
                    return item.Sid;
                }
            }

            return null;
        }

        // 通过季数，搜索结果按年份排序后，取对应季数索引项（不适合每季标题差异太大的，如葫芦兄弟和葫芦小金刚）
        protected async Task<string?> GuestDoubanSeasonByNumberAsync(string name, int? seasonNumber, CancellationToken cancellationToken)
        {
            if (seasonNumber == null || seasonNumber == 0)
            {
                return null;
            }

            this.Log($"GuestDoubanSeasonByNumber of [name]: {name} seasonNumber: {seasonNumber}");
            var result = await this._doubanApi.SearchAsync(name, cancellationToken).ConfigureAwait(false);
            var jw = new JaroWinkler();
            var matchList = new List<DoubanSubject>();
            foreach (var item in result)
            {
                if (item.Category != "电视剧")
                {
                    continue;
                }

                var score = jw.Similarity(name, item.Name);
                if (score < 0.8)
                {
                    continue;
                }

                // this.Log($"GuestDoubanSeasonByNumber name: {name} douban_name: {item.Name} douban_sid: {item.Sid} douban_year: {item.Year} score: {score} ");
                matchList.Add(item);
            }

            matchList.Sort((x, y) => x.Year.CompareTo(y.Year));
            if (matchList.Count >= seasonNumber)
            {
                var matchItem = matchList[seasonNumber.Value - 1];
                var sid = matchItem.Sid;
                this.Log($"GuestDoubanSeasonByNumber of [name] found Sid: {sid}");
                return sid;
            }

            return null;
        }

        protected async Task<string?> GuestByTmdbAsync(ItemLookupInfo info, CancellationToken cancellationToken)
        {
            // ParseName is required here.
            // Caller provides the filename with extension stripped and NOT the parsed filename
            var parsedName = this._libraryManager.ParseName(info.Name);
            this.Log($"GuestByTmdb of [name]: {info.Name} search name: {parsedName.Name}");
            var jw = new JaroWinkler();

            switch (info)
            {
                case MovieInfo:
                    var movieResults = await this._tmdbApi.SearchMovieAsync(parsedName.Name, parsedName.Year ?? 0, info.MetadataLanguage, cancellationToken).ConfigureAwait(false);
                    foreach (var item in movieResults)
                    {
                        if (jw.Similarity(parsedName.Name, item.Title) > 0.8)
                        {
                            this.Log($"GuestByTmdb of [name] found tmdb id: \"{item.Id}\"");
                            return item.Id.ToString(CultureInfo.InvariantCulture);
                        }
                    }
                    break;
                case SeriesInfo:
                    var seriesResults = await this._tmdbApi.SearchSeriesAsync(parsedName.Name, info.MetadataLanguage, cancellationToken).ConfigureAwait(false);
                    foreach (var item in seriesResults)
                    {
                        if (jw.Similarity(parsedName.Name, item.Name) > 0.8)
                        {
                            this.Log($"GuestByTmdb of [name] found tmdb id: \"{item.Id}\"");
                            return item.Id.ToString(CultureInfo.InvariantCulture);
                        }
                    }
                    break;
            }

            return null;
        }

        // 通过imdb获取tmdbId
        protected async Task<string?> GetTmdbIdByImdbAsync(string imdb, string language, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(imdb))
            {
                return null;
            }

            // 通过imdb获取TMDB id (豆瓣的imdb id可能是旧的，需要先从omdb接口获取最新的imdb id
            var omdbItem = await this._omdbApi.GetByImdbID(imdb, cancellationToken).ConfigureAwait(false);
            if (omdbItem != null)
            {
                var findResult = await this._tmdbApi.FindByExternalIdAsync(omdbItem.ImdbID, TMDbLib.Objects.Find.FindExternalSource.Imdb, language, cancellationToken).ConfigureAwait(false);
                if (findResult?.TvResults != null && findResult.TvResults.Count > 0)
                {
                    this.Log($"GetSeriesMetadata found tmdb [id]: {findResult.TvResults[0].Id} by imdb id: {imdb}");
                    return $"{findResult.TvResults[0].Id}";
                }
            }

            return null;
        }



        protected string GetProxyImageUrl(string url)
        {
            var encodedUrl = HttpUtility.UrlEncode(url);
            return $"/plugin/metashark/proxy/image/?url={encodedUrl}";
        }

        protected void Log(string? message, params object?[] args)
        {
            this._logger.LogInformation($"[MetaShark] {message}", args);
        }

        /// <summary>
        /// Adjusts the image's language code preferring the 5 letter language code eg. en-US.
        /// </summary>
        /// <param name="imageLanguage">The image's actual language code.</param>
        /// <param name="requestLanguage">The requested language code.</param>
        /// <returns>The language code.</returns>
        protected string AdjustImageLanguage(string imageLanguage, string requestLanguage)
        {
            if (!string.IsNullOrEmpty(imageLanguage)
                && !string.IsNullOrEmpty(requestLanguage)
                && requestLanguage.Length > 2
                && imageLanguage.Length == 2
                && requestLanguage.StartsWith(imageLanguage, StringComparison.OrdinalIgnoreCase))
            {
                return requestLanguage;
            }

            return imageLanguage;
        }

        /// <summary>
        /// Maps the TMDB provided roles for crew members to Jellyfin roles.
        /// </summary>
        /// <param name="crew">Crew member to map against the Jellyfin person types.</param>
        /// <returns>The Jellyfin person type.</returns>
        [SuppressMessage("Microsoft.Maintainability", "CA1309: Use ordinal StringComparison", Justification = "AFAIK we WANT InvariantCulture comparisons here and not Ordinal")]
        public string MapCrewToPersonType(Crew crew)
        {
            if (crew.Department.Equals("production", StringComparison.InvariantCultureIgnoreCase)
                && crew.Job.Contains("director", StringComparison.InvariantCultureIgnoreCase))
            {
                return PersonType.Director;
            }

            if (crew.Department.Equals("production", StringComparison.InvariantCultureIgnoreCase)
                && crew.Job.Contains("producer", StringComparison.InvariantCultureIgnoreCase))
            {
                return PersonType.Producer;
            }

            if (crew.Department.Equals("writing", StringComparison.InvariantCultureIgnoreCase))
            {
                return PersonType.Writer;
            }

            return string.Empty;
        }


        /// <summary>
        /// Normalizes a language string for use with TMDb's include image language parameter.
        /// </summary>
        /// <param name="preferredLanguage">The preferred language as either a 2 letter code with or without country code.</param>
        /// <returns>The comma separated language string.</returns>
        public static string GetImageLanguagesParam(string preferredLanguage)
        {
            var languages = new List<string>();

            if (!string.IsNullOrEmpty(preferredLanguage))
            {
                preferredLanguage = NormalizeLanguage(preferredLanguage);

                languages.Add(preferredLanguage);

                if (preferredLanguage.Length == 5) // like en-US
                {
                    // Currently, TMDB supports 2-letter language codes only
                    // They are planning to change this in the future, thus we're
                    // supplying both codes if we're having a 5-letter code.
                    languages.Add(preferredLanguage.Substring(0, 2));
                }
            }

            languages.Add("null");

            if (!string.Equals(preferredLanguage, "en", StringComparison.OrdinalIgnoreCase))
            {
                languages.Add("en");
            }

            return string.Join(',', languages);
        }

        /// <summary>
        /// Normalizes a language string for use with TMDb's language parameter.
        /// </summary>
        /// <param name="language">The language code.</param>
        /// <returns>The normalized language code.</returns>
        public static string NormalizeLanguage(string language)
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
    }
}
