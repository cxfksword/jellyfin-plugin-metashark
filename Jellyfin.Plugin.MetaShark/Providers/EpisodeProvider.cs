using System.Reflection.Metadata;
using Jellyfin.Plugin.MetaShark.Api;
using Jellyfin.Plugin.MetaShark.Core;
using Jellyfin.Plugin.MetaShark.Model;
using MediaBrowser.Common.Net;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using Microsoft.AspNetCore.Http;

namespace Jellyfin.Plugin.MetaShark.Providers
{
    public class EpisodeProvider : BaseProvider, IRemoteMetadataProvider<Episode, EpisodeInfo>, IDisposable
    {
        private readonly IMemoryCache _memoryCache;

        private static readonly Regex[] EpisodeFileNameRegex =
        {
            new(@"\[([\d\.]{2,})\]"),
            new(@"- ?([\d\.]{2,})"),
            new(@"EP?([\d\.]{2,})", RegexOptions.IgnoreCase),
            new(@"\[([\d\.]{2,})"),
            new(@"#([\d\.]{2,})"),
            new(@"(\d{2,})")
        };

        public EpisodeProvider(IHttpClientFactory httpClientFactory, ILoggerFactory loggerFactory, ILibraryManager libraryManager, IHttpContextAccessor httpContextAccessor, DoubanApi doubanApi, TmdbApi tmdbApi, OmdbApi omdbApi)
            : base(httpClientFactory, loggerFactory.CreateLogger<EpisodeProvider>(), libraryManager, httpContextAccessor, doubanApi, tmdbApi, omdbApi)
        {
            this._memoryCache = new MemoryCache(new MemoryCacheOptions());
        }

        public string Name => Plugin.PluginName;


        /// <inheritdoc />
        public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(EpisodeInfo info, CancellationToken cancellationToken)
        {
            this.Log($"GetEpisodeSearchResults of [name]: {info.Name}");
            return await Task.FromResult(Enumerable.Empty<RemoteSearchResult>());
        }

        /// <inheritdoc />
        public async Task<MetadataResult<Episode>> GetMetadata(EpisodeInfo info, CancellationToken cancellationToken)
        {
            // 重新识别时，info的IndexNumber和ParentIndexNumber是从文件路径解析出来的，假如命名不规范，就会导致解析出错误值
            // 刷新元数据不覆盖时，IndexNumber和ParentIndexNumber是从当前的元数据获取
            this.Log($"GetEpisodeMetadata of [name]: {info.Name} number: {info.IndexNumber} ParentIndexNumber: {info.ParentIndexNumber}");
            var result = new MetadataResult<Episode>();

            // 动画特典和extras处理
            var specialEpisode = this.HandleAnimeSpecialAndExtras(info.Path);
            if (specialEpisode != null)
            {
                result.HasMetadata = true;
                result.Item = specialEpisode;
                return result;
            }

            // 剧集信息只有tmdb有
            info.SeriesProviderIds.TryGetValue(MetadataProvider.Tmdb.ToString(), out var seriesTmdbId);
            var seasonNumber = info.ParentIndexNumber;
            var episodeNumber = info.IndexNumber;
            var indexNumberEnd = info.IndexNumberEnd;
            // 修正anime命名格式导致的seasonNumber错误（从season元数据读取)
            var parent = _libraryManager.FindByPath(Path.GetDirectoryName(info.Path), true);
            if (parent is Season season && seasonNumber != season.IndexNumber)
            {
                this.Log("FixSeasionNumber: old: {0} new: {1}", seasonNumber, season.IndexNumber);
                seasonNumber = season.IndexNumber;
            }
            // 没有season级目录或目录不命名不规范时，会为null
            if (seasonNumber is null)
            {
                this.Log("FixSeasionNumber: season number is null, set to default 1");
                seasonNumber = 1;
            }
            // 修正anime命名格式导致的episodeNumber错误
            var fileName = Path.GetFileNameWithoutExtension(info.Path) ?? string.Empty;
            var guessInfo = this.GuessEpisodeNumber(fileName);
            this.Log("GuessEpisodeNumber: fileName: {0} seasonNumber: {1} episodeNumber: {2} name: {3}", fileName, guessInfo.seasonNumber, guessInfo.episodeNumber, guessInfo.Name);
            if (guessInfo.seasonNumber.HasValue && guessInfo.seasonNumber != seasonNumber)
            {
                seasonNumber = guessInfo.seasonNumber.Value;
            }
            if (guessInfo.episodeNumber.HasValue)
            {
                episodeNumber = guessInfo.episodeNumber;

                result.HasMetadata = true;
                result.Item = new Episode
                {
                    ParentIndexNumber = seasonNumber,
                    IndexNumber = episodeNumber
                };
                if (!string.IsNullOrEmpty(guessInfo.Name))
                {
                    result.Item.Name = guessInfo.Name;
                }
            }

            if (episodeNumber is null or 0 || seasonNumber is null or 0 || string.IsNullOrEmpty(seriesTmdbId))
            {
                this.Log("Lack meta message. episodeNumber: {0} seasonNumber: {1} seriesTmdbId:{2}", episodeNumber, seasonNumber, seriesTmdbId);
                return result;
            }

            // 利用season缓存取剧集信息会更快
            var seasonResult = await this._tmdbApi
                .GetSeasonAsync(seriesTmdbId.ToInt(), seasonNumber.Value, info.MetadataLanguage, info.MetadataLanguage, cancellationToken)
                .ConfigureAwait(false);
            if (seasonResult == null || seasonResult.Episodes.Count < episodeNumber.Value)
            {
                this.Log("Can‘t found episode data from tmdb. Name: {0} seriesTmdbId: {1} seasonNumber: {2} episodeNumber: {3}", info.Name, seriesTmdbId, seasonNumber, episodeNumber);
                return result;
            }

            // 判断tmdb剧集信息数目和视频是否一致，不一致不处理
            var videoFilesCount = this.GetVideoFileCount(Path.GetDirectoryName(info.Path));
            if (!info.IsAutomated && parent is Season)
            {
                // 刷新元数据时，直接从season拿准确的视频数，并排除特典等没有季号的视频
                videoFilesCount = ((Season)parent).GetEpisodes().Where(x => x.ParentIndexNumber == parent.IndexNumber).Count();
            }
            if (videoFilesCount > 0 && seasonResult.Episodes.Count != videoFilesCount)
            {
                this.Log("Tmdb episode number not match. Name: {0} tmdb episode count: {1} video files count: {2}", info.Name, seasonResult.Episodes.Count, videoFilesCount);
                return result;
            }

            var episodeResult = seasonResult.Episodes[episodeNumber.Value - 1];

            result.HasMetadata = true;
            result.QueriedById = true;

            if (!string.IsNullOrEmpty(episodeResult.Overview))
            {
                // if overview is non-empty, we can assume that localized data was returned
                result.ResultLanguage = info.MetadataLanguage;
            }

            var item = new Episode
            {
                IndexNumber = episodeNumber,
                ParentIndexNumber = seasonNumber,
                IndexNumberEnd = info.IndexNumberEnd
            };


            item.PremiereDate = episodeResult.AirDate;
            item.ProductionYear = episodeResult.AirDate?.Year;
            item.Name = episodeResult.Name;
            item.Overview = episodeResult.Overview;
            item.CommunityRating = (float)System.Math.Round(episodeResult.VoteAverage, 1);

            result.Item = item;

            return result;
        }

        /// <inheritdoc />
        public Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            this.Log("GetImageResponse url: {0}", url);
            return _httpClientFactory.CreateClient().GetAsync(new Uri(url), cancellationToken);
        }

        public GuessInfo GuessEpisodeNumber(string fileName, double max = double.PositiveInfinity)
        {
            var guessInfo = new GuessInfo();

            var parseResult = AnitomySharp.AnitomySharp.Parse(fileName);
            var animeSpecialType = parseResult.FirstOrDefault(x => x.Category == AnitomySharp.Element.ElementCategory.ElementAnimeType && x.Value == "SP");
            if (animeSpecialType != null)
            {
                guessInfo.seasonNumber = 0;
            }
            var animeEpisode = parseResult.FirstOrDefault(x => x.Category == AnitomySharp.Element.ElementCategory.ElementEpisodeNumber);
            if (animeEpisode != null)
            {
                guessInfo.episodeNumber = animeEpisode.Value.ToInt();
            }

            if (!guessInfo.episodeNumber.HasValue)
            {
                foreach (var regex in EpisodeFileNameRegex)
                {
                    if (!regex.IsMatch(fileName))
                        continue;
                    if (!int.TryParse(regex.Match(fileName).Groups[1].Value.Trim('.'), out var index))
                        continue;
                    guessInfo.episodeNumber = index;
                    break;
                }
            }

            if (guessInfo.episodeNumber > 1000)
            {
                // 可能解析了分辨率，忽略返回
                guessInfo.episodeNumber = null;
            }

            var animeName = parseResult.FirstOrDefault(x => x.Category == AnitomySharp.Element.ElementCategory.ElementAnimeTitle);
            if (animeName != null && NameParser.IsAnime(fileName))
            {
                guessInfo.Name = animeName.Value;
            }

            return guessInfo;
        }

        private Episode? HandleAnimeSpecialAndExtras(string filePath)
        {
            var fileName = Path.GetFileNameWithoutExtension(filePath) ?? string.Empty;
            if (NameParser.IsExtra(fileName))
            {
                this.Log($"Found anime extra of [name]: {fileName}");
                return new Episode
                {
                    Name = fileName
                };
            }
            if (NameParser.IsSpecial(filePath))
            {
                this.Log($"Found anime sp of [name]: {fileName}");
                var guessInfo = this.GuessEpisodeNumber(fileName);
                var ep = new Episode
                {
                    ParentIndexNumber = 0,
                    IndexNumber = guessInfo.episodeNumber,
                };
                if (!string.IsNullOrEmpty(guessInfo.Name))
                {
                    ep.Name = guessInfo.Name;
                }

                return ep;
            }

            return null;
        }



        protected int GetVideoFileCount(string? dir)
        {
            if (dir == null)
            {
                return 0;
            }

            var cacheKey = $"filecount_{dir}";
            if (this._memoryCache.TryGetValue<int>(cacheKey, out var videoFilesCount))
            {
                return videoFilesCount;
            }

            var dirInfo = new DirectoryInfo(dir);
            var files = dirInfo.GetFiles();
            var nameOptions = new Emby.Naming.Common.NamingOptions();

            foreach (var fileInfo in files.Where(f => !f.Attributes.HasFlag(FileAttributes.Hidden)))
            {
                if (Emby.Naming.Video.VideoResolver.IsVideoFile(fileInfo.FullName, nameOptions))
                {
                    videoFilesCount++;
                }
            }

            var expiredOption = new MemoryCacheEntryOptions() { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1) };
            this._memoryCache.Set<int>(cacheKey, videoFilesCount, expiredOption);
            return videoFilesCount;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _memoryCache.Dispose();
            }
        }
    }
}
