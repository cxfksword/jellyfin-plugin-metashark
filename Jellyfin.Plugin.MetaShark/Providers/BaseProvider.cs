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
using Microsoft.AspNetCore.Http;

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
        protected readonly IHttpContextAccessor _httpContextAccessor;

        protected Regex regMetaSourcePrefix = new Regex(@"^\[.+\]", RegexOptions.Compiled);
        protected Regex regSeasonNameSuffix = new Regex(@"\s第[0-9一二三四五六七八九十]+?季$", RegexOptions.Compiled);

        protected PluginConfiguration config
        {
            get
            {
                return Plugin.Instance?.Configuration ?? new PluginConfiguration();
            }
        }

        protected string RequestDomain
        {
            get
            {
                if (_httpContextAccessor.HttpContext != null)
                {
                    return _httpContextAccessor.HttpContext.Request.Scheme + System.Uri.SchemeDelimiter + _httpContextAccessor.HttpContext.Request.Host;
                }
                else
                {
                    return string.Empty;
                }

            }
        }

        protected string RequestPath
        {
            get
            {
                if (_httpContextAccessor.HttpContext != null)
                {
                    return _httpContextAccessor.HttpContext.Request.Path.ToString();
                }
                else
                {
                    return string.Empty;
                }

            }
        }

        protected BaseProvider(IHttpClientFactory httpClientFactory, ILogger logger, ILibraryManager libraryManager, IHttpContextAccessor httpContextAccessor, DoubanApi doubanApi, TmdbApi tmdbApi, OmdbApi omdbApi)
        {
            this._doubanApi = doubanApi;
            this._tmdbApi = tmdbApi;
            this._omdbApi = omdbApi;
            this._libraryManager = libraryManager;
            this._logger = logger;
            this._httpClientFactory = httpClientFactory;
            this._httpContextAccessor = httpContextAccessor;
        }

        protected async Task<string?> GuessByDoubanAsync(ItemLookupInfo info, CancellationToken cancellationToken)
        {
            var fileName = GetOriginalFileName(info);
            var parseResult = NameParser.Parse(fileName);
            var searchName = !string.IsNullOrEmpty(parseResult.ChineseName) ? parseResult.ChineseName : parseResult.Name;
            info.Year = parseResult.Year;  // 默认parser对anime年份会解析出错，以anitomy为准


            this.Log($"GuessByDouban of [name]: {info.Name} [file_name]: {fileName} [year]: {info.Year} [search name]: {searchName}");
            List<DoubanSubject> result;
            DoubanSubject? item;

            // 假如存在年份，先通过suggest接口查找，减少搜索页访问次数，避免封禁（suggest没法区分电影或电视剧，排序也比搜索页差些）
            if (config.EnableDoubanAvoidRiskControl)
            {
                if (info.Year != null && info.Year > 0)
                {
                    result = await this._doubanApi.SearchBySuggestAsync(searchName, cancellationToken).ConfigureAwait(false);
                    item = result.Where(x => x.Year == info.Year && x.Name == searchName).FirstOrDefault();
                    if (item != null)
                    {
                        this.Log($"GuessByDouban found -> {item.Name}({item.Sid}) (suggest)");
                        return item.Sid;
                    }
                    item = result.Where(x => x.Year == info.Year).FirstOrDefault();
                    if (item != null)
                    {
                        this.Log($"GuessByDouban found -> {item.Name}({item.Sid}) (suggest)");
                        return item.Sid;
                    }
                }
            }

            // 通过搜索页面查找
            result = await this._doubanApi.SearchAsync(searchName, cancellationToken).ConfigureAwait(false);
            var cat = info is MovieInfo ? "电影" : "电视剧";

            // 优先返回对应年份的电影
            if (info.Year != null && info.Year > 0)
            {
                item = result.Where(x => x.Category == cat && x.Year == info.Year).FirstOrDefault();
                if (item != null)
                {
                    this.Log($"Found douban [id]: {item.Name}({item.Sid})");
                    return item.Sid;
                }
            }

            //// 不存在年份，计算相似度，返回相似度大于0.8的第一个（可能出现冷门资源名称更相同的情况。。。）
            // var jw = new JaroWinkler();
            // item = result.Where(x => x.Category == cat && x.Rating > 5).OrderByDescending(x => Math.Max(jw.Similarity(searchName, x.Name), jw.Similarity(searchName, x.OriginalName))).FirstOrDefault();
            // if (item != null && Math.Max(jw.Similarity(searchName, item.Name), jw.Similarity(searchName, item.OriginalName)) > 0.8)
            // {
            //     return item.Sid;
            // }

            // 不存在年份时，返回豆瓣结果第一个
            item = result.Where(x => x.Category == cat).FirstOrDefault();
            if (item != null)
            {
                this.Log($"GuessByDouban found -> {item.Name}({item.Sid})");
                return item.Sid;
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

            // 先通过suggest接口查找，减少搜索页访问次数，避免封禁（suggest没法区分电影或电视剧，排序也比搜索页差些）
            if (config.EnableDoubanAvoidRiskControl)
            {
                var suggestResult = await this._doubanApi.SearchBySuggestAsync(name, cancellationToken).ConfigureAwait(false);
                var suggestItem = suggestResult.Where(x => x.Year == year && x.Name == name).FirstOrDefault();
                if (suggestItem != null)
                {
                    this.Log($"Found douban [id]: {suggestItem.Name}({suggestItem.Sid}) (suggest)");
                    return suggestItem.Sid;
                }
                suggestItem = suggestResult.Where(x => x.Year == year).FirstOrDefault();
                if (suggestItem != null)
                {
                    this.Log($"Found douban [id]: {suggestItem.Name}({suggestItem.Sid}) (suggest)");
                    return suggestItem.Sid;
                }
            }


            // 通过搜索页面查找
            var result = await this._doubanApi.SearchAsync(name, cancellationToken).ConfigureAwait(false);
            var item = result.Where(x => x.Category == "电视剧" && x.Year == year).FirstOrDefault();
            if (item != null && !string.IsNullOrEmpty(item.Sid))
            {
                this.Log($"Found douban [id]: {item.Name}({item.Sid})");
                return item.Sid;
            }

            this.Log($"GuestDoubanSeasonByYear not found!");
            return null;
        }


        protected async Task<string?> GuestByTmdbAsync(string name, int? year, ItemLookupInfo info, CancellationToken cancellationToken)
        {
            var fileName = GetOriginalFileName(info);

            this.Log($"GuestByTmdb of [name]: {name} [year]: {year}");
            switch (info)
            {
                case MovieInfo:
                    var movieResults = await this._tmdbApi.SearchMovieAsync(name, year ?? 0, info.MetadataLanguage, cancellationToken).ConfigureAwait(false);
                    var movieItem = movieResults.FirstOrDefault();
                    if (movieItem != null)
                    {
                        // bt种子都是英文名，但电影是中日韩泰印法地区时，都不适用相似匹配，去掉限制
                        this.Log($"Found tmdb [id]: {movieItem.Title}({movieItem.Id})");
                        return movieItem.Id.ToString(CultureInfo.InvariantCulture);
                    }
                    break;
                case SeriesInfo:
                    var seriesResults = await this._tmdbApi.SearchSeriesAsync(name, info.MetadataLanguage, cancellationToken).ConfigureAwait(false);
                    var seriesItem = seriesResults.FirstOrDefault();
                    if (seriesItem != null)
                    {
                        // bt种子都是英文名，但电影是中日韩泰印法地区时，都不适用相似匹配，去掉限制
                        this.Log($"Found tmdb [id]: -> {seriesItem.Name}({seriesItem.Id})");
                        return seriesItem.Id.ToString(CultureInfo.InvariantCulture);
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
            if (!string.IsNullOrEmpty(omdbItem?.ImdbID))
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


        /// <summary>
        /// 浏览器来源请求，返回代理地址（no-referer对于background-image不生效），其他客户端请求，返回原始图片地址
        /// </summary>
        protected string GetProxyImageUrl(string url)
        {
            var fromWeb = false;
            if (_httpContextAccessor.HttpContext != null)
            {
                var userAgent = _httpContextAccessor.HttpContext.Request.Headers.UserAgent.ToString();
                fromWeb = userAgent.Contains("Chrome") || userAgent.Contains("Safari");
            }

            if (fromWeb)
            {
                var encodedUrl = HttpUtility.UrlEncode(url);
                return $"/plugin/metashark/proxy/image/?url={encodedUrl}";
            }
            else
            {
                return url;
            }
        }


        protected string GetAbsoluteProxyImageUrl(string url)
        {
            var encodedUrl = HttpUtility.UrlEncode(url);
            return $"{this.RequestDomain}/plugin/metashark/proxy/image/?url={encodedUrl}";
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

        protected string GetOriginalFileName(ItemLookupInfo info)
        {
            switch (info)
            {
                case SeriesInfo:
                case SeasonInfo:
                    return Path.GetFileNameWithoutExtension(info.Path) ?? info.Name;
                default:
                    return Path.GetFileNameWithoutExtension(info.Path) ?? info.Name;
            }
        }

        protected string RemoveSeasonSubfix(string name)
        {
            return regSeasonNameSuffix.Replace(name, "");
        }
    }
}
