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
            var fileName = info.Name;
            var parseResult = NameParser.Parse(fileName);
            var searchName = !string.IsNullOrEmpty(parseResult.ChineseName) ? parseResult.ChineseName : parseResult.Name;
            info.Year = parseResult.Year;  // 默认parser对anime年份会解析出错，以anitomy为准


            this.Log($"GuessByDouban of [name]: {info.Name} [year]: {info.Year} [search name]: {searchName}");
            var result = await this._doubanApi.SearchAsync(searchName, cancellationToken).ConfigureAwait(false);
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


                // bt种子都是英文名，但电影是中日韩泰印法地区时，都不适用相似匹配，去掉限制
                // //英文关键词搜，结果是只有中文/繁体中文时，不适用相似匹配，如Who Am I
                // if (jw.Similarity(searchName, item.Name) < 0.8 && jw.Similarity(searchName, item.OriginalName) < 0.8)
                // {
                //     if (!searchName.IsSameLanguage(item.Name) && !searchName.IsSameLanguage(item.OriginalName))
                //     {
                //         // 特殊处理下使用英文搜索，只有中文标题的情况
                //     }
                //     else
                //     {
                //         continue;
                //     }
                // }

                // 不存在年份需要比较时，直接返回
                if (info.Year == null || info.Year == 0)
                {
                    this.Log($"GuessByDouban of [name] found Sid: {item.Sid}");
                    return item.Sid;
                }

                if (info.Year == item.Year)
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

            this.Log($"GuestDoubanSeasonByYear of [name]: {name} [year]: {year}");
            var result = await this._doubanApi.SearchAsync(name, cancellationToken).ConfigureAwait(false);
            var jw = new JaroWinkler();
            foreach (var item in result)
            {
                if (item.Category != "电视剧")
                {
                    continue;
                }

                // bt种子都是英文名，但电影是中日韩泰印法地区时，都不适用相似匹配，去掉限制
                // // this.Log($"GuestDoubanSeasonByYear name: {name} douban_name: {item.Name} douban_sid: {item.Sid} douban_year: {item.Year} score: {score} ");
                // //英文关键词搜，结果是只有中文/繁体中文时，不适用相似匹配，如Who Am I
                // if (jw.Similarity(name, item.Name) < 0.8 && jw.Similarity(name, item.OriginalName) < 0.8)
                // {
                //     if (!name.IsSameLanguage(item.Name) && !name.IsSameLanguage(item.OriginalName))
                //     {
                //         // 特殊处理下使用英文搜索，只有中文标题的情况
                //     }
                //     else
                //     {
                //         continue;
                //     }
                // }

                if (year == item.Year)
                {
                    this.Log($"GuestDoubanSeasonByYear of [name] found Sid: \"{item.Sid}\"");
                    return item.Sid;
                }
            }

            return null;
        }


        protected async Task<string?> GuestByTmdbAsync(ItemLookupInfo info, CancellationToken cancellationToken)
        {
            // ParseName is required here.
            // Caller provides the filename with extension stripped and NOT the parsed filename
            var fileName = info.Name;
            var parseResult = NameParser.Parse(fileName);
            var searchName = !string.IsNullOrEmpty(parseResult.ChineseName) ? parseResult.ChineseName : parseResult.Name;
            info.Year = parseResult.Year;  // 默认parser对anime年份会解析出错，以anitomy为准

            this.Log($"GuestByTmdb of [name]: {info.Name} [year]: {info.Year} [search name]: {searchName}");
            var jw = new JaroWinkler();

            switch (info)
            {
                case MovieInfo:
                    var movieResults = await this._tmdbApi.SearchMovieAsync(searchName, info.Year ?? 0, info.MetadataLanguage, cancellationToken).ConfigureAwait(false);
                    foreach (var item in movieResults)
                    {
                        // bt种子都是英文名，但电影是中日韩泰印法地区时，都不适用相似匹配，去掉限制
                        // if (jw.Similarity(searchName, item.Title) > 0.8
                        //     || jw.Similarity(searchName, item.OriginalTitle) > 0.8)
                        // {
                        //     this.Log($"GuestByTmdb of [name] found tmdb id: \"{item.Id}\"");
                        //     return item.Id.ToString(CultureInfo.InvariantCulture);
                        // }
                        // // 特殊处理下使用英文搜索，只有中文标题的情况，当匹配成功
                        // if (!searchName.IsSameLanguage(item.Title) && !searchName.IsSameLanguage(item.OriginalTitle))
                        // {
                        //     this.Log($"GuestByTmdb of [name] found tmdb id: \"{item.Id}\"");
                        //     return item.Id.ToString(CultureInfo.InvariantCulture);
                        // }

                        return item.Id.ToString(CultureInfo.InvariantCulture);
                    }
                    break;
                case SeriesInfo:
                    var seriesResults = await this._tmdbApi.SearchSeriesAsync(searchName, info.MetadataLanguage, cancellationToken).ConfigureAwait(false);
                    foreach (var item in seriesResults)
                    {
                        // bt种子都是英文名，但电影是中日韩泰印法地区时，都不适用相似匹配，去掉限制
                        // if (jw.Similarity(searchName, item.Name) > 0.8
                        //      || jw.Similarity(searchName, item.OriginalName) > 0.8)
                        // {
                        //     this.Log($"GuestByTmdb of [name] found tmdb id: \"{item.Id}\"");
                        //     return item.Id.ToString(CultureInfo.InvariantCulture);
                        // }
                        // // 特殊处理下使用英文搜索，只有中文标题的情况，当匹配成功
                        // if (!searchName.IsSameLanguage(item.Name) && !searchName.IsSameLanguage(item.OriginalName))
                        // {
                        //     this.Log($"GuestByTmdb of [name] found tmdb id: \"{item.Id}\"");
                        //     return item.Id.ToString(CultureInfo.InvariantCulture);
                        // }

                        return item.Id.ToString(CultureInfo.InvariantCulture);
                    }
                    break;
            }

            return null;
        }


        protected async Task<string?> GetTmdbIdByImdbAsync(string imdb, string language, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(imdb))
            {
                return null;
            }

            // 豆瓣的imdb id可能是旧的，需要先从omdb接口获取最新的imdb id
            var omdbItem = await this._omdbApi.GetByImdbID(imdb, cancellationToken).ConfigureAwait(false);
            if (omdbItem != null)
            {
                imdb = omdbItem.ImdbID;
            }

            // 通过imdb获取tmdbId
            var findResult = await this._tmdbApi.FindByExternalIdAsync(imdb, TMDbLib.Objects.Find.FindExternalSource.Imdb, language, cancellationToken).ConfigureAwait(false);
            if (findResult?.MovieResults != null && findResult.MovieResults.Count > 0)
            {
                var tmdbId = findResult.MovieResults[0].Id;
                this.Log($"Found tmdb [id]: {tmdbId} by imdb id: {imdb}");
                return $"{tmdbId}";
            }

            if (findResult?.TvResults != null && findResult.TvResults.Count > 0)
            {
                var tmdbId = findResult.TvResults[0].Id;
                this.Log($"Found tmdb [id]: {tmdbId} by imdb id: {imdb}");
                return $"{tmdbId}";
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
