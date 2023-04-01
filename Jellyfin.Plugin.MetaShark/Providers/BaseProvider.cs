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
        protected Regex regSeasonNameSuffix = new Regex(@"\s第[0-9一二三四五六七八九十]+?季$|\sSeason\s\d+?$|(?<![0-9a-zA-Z])\d$", RegexOptions.Compiled);

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

            // 存在年份时，返回对应年份的电影
            if (info.Year != null && info.Year > 0)
            {
                item = result.Where(x => x.Category == cat && x.Year == info.Year).FirstOrDefault();
                if (item != null)
                {
                    this.Log($"Found douban [id]: {item.Name}({item.Sid})");
                    return item.Sid;
                }
                else
                {
                    // TODO: 有年份找不到，直接返回，由其他插件接手查找（还是返回第一个好？？？？）
                    return null;
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

        public async Task<string?> GuestDoubanSeasonByYearAsync(string seriesName, int? year, int? seasonNumber, CancellationToken cancellationToken)
        {
            if (year == null || year == 0)
            {
                return null;
            }

            this.Log($"GuestDoubanSeasonByYear of [name]: {seriesName} [year]: {year}");

            // 先通过suggest接口查找，减少搜索页访问次数，避免封禁（suggest没法区分电影或电视剧，排序也比搜索页差些）
            if (config.EnableDoubanAvoidRiskControl)
            {
                var suggestResult = await this._doubanApi.SearchBySuggestAsync(seriesName, cancellationToken).ConfigureAwait(false);
                var suggestItem = suggestResult.Where(x => x.Year == year && x.Name == seriesName).FirstOrDefault();
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
            var result = await this._doubanApi.SearchAsync(seriesName, cancellationToken).ConfigureAwait(false);
            var item = result.Where(x => x.Category == "电视剧" && x.Year == year).FirstOrDefault();
            if (item != null && !string.IsNullOrEmpty(item.Sid))
            {
                // 判断名称中是否有第X季，有的话和seasonNumber比较，用于修正多季都在同一年时，每次都是错误取第一个的情况
                var nameIndexNumber = ParseChineseSeasonNumberByName(item.Name);
                if (nameIndexNumber.HasValue && seasonNumber.HasValue && nameIndexNumber != seasonNumber)
                {
                    this.Log($"GuestDoubanSeasonByYear not found!");
                    return null;
                }

                this.Log($"Found douban [id]: {item.Name}({item.Sid})");
                return item.Sid;
            }

            this.Log($"GuestDoubanSeasonByYear not found!");
            return null;
        }

        public async Task<string?> GuestDoubanSeasonBySeasonNameAsync(string name, int? seasonNumber, CancellationToken cancellationToken)
        {
            if (seasonNumber is null or 0)
            {
                return null;
            }

            var chineseSeasonNumber = Utils.ToChineseNumber(seasonNumber);
            if (string.IsNullOrEmpty(chineseSeasonNumber))
            {
                return null;
            }

            var seasonName = $"{name}{seasonNumber}";
            var chineseSeasonName = $"{name} 第{chineseSeasonNumber}季";
            if (seasonNumber == 1)
            {
                seasonName = name;
            }
            this.Log($"GuestDoubanSeasonBySeasonNameAsync of [name]: {seasonName} 或 {chineseSeasonName}");

            // 通过名称精确匹配
            var result = await this._doubanApi.SearchAsync(name, cancellationToken).ConfigureAwait(false);
            var item = result.Where(x => x.Category == "电视剧" && x.Rating > 0 && (x.Name == seasonName || x.Name == chineseSeasonName)).FirstOrDefault();
            if (item != null && !string.IsNullOrEmpty(item.Sid))
            {
                this.Log($"Found douban [id]: {item.Name}({item.Sid})");
                return item.Sid;
            }


            this.Log($"GuestDoubanSeasonBySeasonNameAsync not found!");
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


        public int? GuessSeasonNumberByDirectoryName(string path)
        {
            // TODO: 有时series name中会带有季信息
            // 当没有season级目录时，path为空，直接返回
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }


            var fileName = Path.GetFileName(path);
            if (string.IsNullOrEmpty(fileName))
            {
                return null;
            }

            var regSeason = new Regex(@"第([0-9零一二三四五六七八九]+?)(季|部)", RegexOptions.Compiled);
            var match = regSeason.Match(fileName);
            if (match.Success && match.Groups.Count > 1)
            {
                var seasonNumber = match.Groups[1].Value.ToInt();
                if (seasonNumber <= 0)
                {
                    seasonNumber = Utils.ChineseNumberToInt(match.Groups[1].Value) ?? 0;
                }
                if (seasonNumber > 0)
                {
                    this.Log($"Found season number of filename: {fileName} seasonNumber: {seasonNumber}");
                    return seasonNumber;
                }
            }


            var seasonNameMap = new Dictionary<string, int>() {
                {@"[ ._](I|1st|S01|S1)[ ._]", 1},
                {@"[ ._](II|2nd|S02|S2)[ ._]", 2},
                {@"[ ._](III|3rd|S03|S3)[ ._]", 3},
                {@"[ ._](IIII|4th|S04|S4)[ ._]", 3},
            };

            foreach (var entry in seasonNameMap)
            {
                if (Regex.IsMatch(fileName, entry.Key))
                {
                    this.Log($"Found season number of filename: {fileName} seasonNumber: {entry.Value}");
                    return entry.Value;
                }
            }

            // // 带数字末尾的
            // match = Regex.Match(fileName, @"[ ._](\d{1,2})$");
            // if (match.Success && match.Groups.Count > 1)
            // {
            //     var seasonNumber = match.Groups[1].Value.ToInt();
            //     if (seasonNumber > 0)
            //     {
            //         this.Log($"Found season number of filename: {fileName} seasonNumber: {seasonNumber}");
            //         return seasonNumber;
            //     }
            // }

            return null;
        }


        public int? ParseChineseSeasonNumberByName(string name)
        {
            var regSeason = new Regex(@"\s第([0-9零一二三四五六七八九]+?)(季|部)", RegexOptions.Compiled);
            var match = regSeason.Match(name);
            if (match.Success && match.Groups.Count > 1)
            {
                var seasonNumber = match.Groups[1].Value.ToInt();
                if (seasonNumber <= 0)
                {
                    seasonNumber = Utils.ChineseNumberToInt(match.Groups[1].Value) ?? 0;
                }
                if (seasonNumber > 0)
                {
                    return seasonNumber;
                }
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


        protected string GetOriginalFileName(ItemLookupInfo info)
        {
            switch (info)
            {
                case MovieInfo:
                    // 当movie放在文件夹中并只有一部影片时, info.name是根据文件夹名解析的，但info.Path是影片的路径名
                    // 当movie放在文件夹中并有多部影片时，info.Name和info.Path都是具体的影片
                    var directoryName = Path.GetFileName(Path.GetDirectoryName(info.Path));
                    if (directoryName != null && directoryName.Contains(info.Name))
                    {
                        return directoryName;
                    }
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
