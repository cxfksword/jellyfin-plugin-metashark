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
            // 刷新元数据四种模式差别：
            // 自动扫描匹配：info的Name、IndexNumber和ParentIndexNumber是从文件名解析出来的，假如命名不规范，就会导致解析出错误值
            // 识别：info的Name、IndexNumber和ParentIndexNumber是从文件名解析出来的，provinceIds有指定选择项的ProvinceId
            // 搜索缺少的元数据：info的Name、IndexNumber和ParentIndexNumber是从当前的元数据获取，provinceIds保留所有旧值
            // 覆盖所有元数据：info的Name、IndexNumber和ParentIndexNumber是从当前的元数据获取，provinceIds保留所有旧值
            this.Log($"GetEpisodeMetadata of [name]: {info.Name} number: {info.IndexNumber} ParentIndexNumber: {info.ParentIndexNumber} IsAutomated: {info.IsAutomated}");
            var result = new MetadataResult<Episode>();

            // 动画特典和extras处理
            var specialResult = this.HandleAnimeSpecialAndExtras(info);
            if (specialResult != null)
            {
                return specialResult;
            }

            // 使用AnitomySharp进行重新解析，解决anime识别错误
            info = this.FixParseInfo(info);

            // 剧集信息只有tmdb有
            info.SeriesProviderIds.TryGetValue(MetadataProvider.Tmdb.ToString(), out var seriesTmdbId);
            var seasonNumber = info.ParentIndexNumber;
            var episodeNumber = info.IndexNumber;
            result.HasMetadata = true;
            result.Item = new Episode
            {
                ParentIndexNumber = seasonNumber,
                IndexNumber = episodeNumber,
                Name = info.Name,
            };

            if (episodeNumber is null or 0 || seasonNumber is null || string.IsNullOrEmpty(seriesTmdbId))
            {
                this.Log("Lack meta data. episodeNumber: {0} seasonNumber: {1} seriesTmdbId:{2}", episodeNumber, seasonNumber, seriesTmdbId);
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

            // TODO：自动搜索匹配或识别时，判断tmdb剧集信息数目和视频是否一致，不一致不处理（现在通过IsAutomated判断不太准确）
            if (info.IsAutomated)
            {
                var videoFilesCount = this.GetVideoFileCount(Path.GetDirectoryName(info.Path));
                if (videoFilesCount > 0 && seasonResult.Episodes.Count != videoFilesCount)
                {
                    this.Log("Tmdb episode number not match. Name: {0} tmdb episode count: {1} video files count: {2}", info.Name, seasonResult.Episodes.Count, videoFilesCount);
                    return result;
                }
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
            };


            item.PremiereDate = episodeResult.AirDate;
            item.ProductionYear = episodeResult.AirDate?.Year;
            item.Name = episodeResult.Name;
            item.Overview = episodeResult.Overview;
            item.CommunityRating = (float)System.Math.Round(episodeResult.VoteAverage, 1);

            result.Item = item;

            return result;
        }

        public EpisodeInfo FixParseInfo(EpisodeInfo info)
        {
            // 使用AnitomySharp进行重新解析，解决anime识别错误
            var fileName = Path.GetFileNameWithoutExtension(info.Path) ?? info.Name;
            var parseResult = NameParser.Parse(fileName);
            info.Year = parseResult.Year;
            info.Name = parseResult.Name;

            // 没有season级目录或文件命名不规范时，ParentIndexNumber会为null
            if (info.ParentIndexNumber is null)
            {
                info.ParentIndexNumber = parseResult.ParentIndexNumber;
            }

            // 修正anime命名格式导致的seasonNumber错误（从season元数据读取)
            if (info.ParentIndexNumber is null)
            {
                var episodeItem = _libraryManager.FindByPath(info.Path, false);
                var season = episodeItem != null ? ((Episode)episodeItem).Season : null;
                if (season != null && info.ParentIndexNumber != season.IndexNumber)
                {
                    this.Log("FixSeasonNumber: old: {0} new: {1}", info.ParentIndexNumber, season.IndexNumber);
                    info.ParentIndexNumber = season.IndexNumber;
                }


                // 当没有season级目录时，默认为1，即当成只有一季
                // if (info.ParentIndexNumber is null && season != null && season.LocationType == LocationType.Virtual)
                // {
                //     this.Log("FixSeasonNumber: season is virtual, set to default 1");
                //     info.ParentIndexNumber = 1;
                // }
            }

            // 设为默认季数为1
            if (info.ParentIndexNumber is null)
            {
                this.Log("FixSeasonNumber: season number is null, set to default 1");
                info.ParentIndexNumber = 1;
            }

            // 特典
            if (NameParser.IsAnime(fileName) && parseResult.IsSpecial)
            {
                info.ParentIndexNumber = 0;
            }

            // 特典优先使用文件名
            if (info.ParentIndexNumber.HasValue && info.ParentIndexNumber == 0)
            {
                info.Name = parseResult.SpecialName == info.Name ? fileName : parseResult.SpecialName;
            }


            // 大于1000，可能错误解析了分辨率
            if (parseResult.IndexNumber.HasValue && parseResult.IndexNumber < 1000)
            {
                info.IndexNumber = parseResult.IndexNumber;
            }

            this.Log("FixParseInfo: fileName: {0} seasonNumber: {1} episodeNumber: {2} name: {3}", fileName, info.ParentIndexNumber, info.IndexNumber, info.Name);
            return info;
        }


        private MetadataResult<Episode>? HandleAnimeSpecialAndExtras(EpisodeInfo info)
        {
            // 特典或extra视频可能和正片剧集放在同一目录
            var fileName = Path.GetFileNameWithoutExtension(info.Path) ?? info.Name;
            var parseResult = NameParser.Parse(fileName);
            if (parseResult.IsExtra)
            {
                this.Log($"Found anime extra of [name]: {fileName}");
                var result = new MetadataResult<Episode>();
                result.HasMetadata = true;

                // 假如已有ParentIndexNumber，设为特典覆盖掉（设为null不会替换旧值）
                if (info.ParentIndexNumber.HasValue)
                {
                    result.Item = new Episode
                    {
                        ParentIndexNumber = 0,
                        IndexNumber = null,
                        Name = parseResult.ExtraName,
                        AirsAfterSeasonNumber = 1,
                    };
                    return result;
                }

                // 没ParentIndexNumber时只修改名称
                result.Item = new Episode
                {
                    Name = parseResult.ExtraName,
                    AirsAfterSeasonNumber = 1,
                };
                return result;
            }

            if (parseResult.IsSpecial || NameParser.IsSpecialDirectory(info.Path))
            {
                this.Log($"Found anime sp of [name]: {fileName}");
                var result = new MetadataResult<Episode>();
                result.HasMetadata = true;
                result.Item = new Episode
                {
                    ParentIndexNumber = 0,
                    IndexNumber = parseResult.IndexNumber,
                    Name = parseResult.SpecialName == info.Name ? fileName : parseResult.SpecialName,
                    AirsAfterSeasonNumber = 1,
                };

                return result;
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
            if (dirInfo == null)
            {
                return 0;
            }
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

        /// <inheritdoc />
        public Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            this.Log("GetImageResponse url: {0}", url);
            return _httpClientFactory.CreateClient().GetAsync(new Uri(url), cancellationToken);
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
