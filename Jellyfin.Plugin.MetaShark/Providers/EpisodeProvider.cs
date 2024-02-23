using Jellyfin.Plugin.MetaShark.Api;
using Jellyfin.Plugin.MetaShark.Core;
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
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Jellyfin.Plugin.MetaShark.Providers
{
    public class EpisodeProvider : BaseProvider, IRemoteMetadataProvider<Episode, EpisodeInfo>, IDisposable
    {
        private readonly IMemoryCache _memoryCache;

        public EpisodeProvider(IHttpClientFactory httpClientFactory, ILoggerFactory loggerFactory, ILibraryManager libraryManager, IHttpContextAccessor httpContextAccessor, DoubanApi doubanApi, TmdbApi tmdbApi, OmdbApi omdbApi, ImdbApi imdbApi)
            : base(httpClientFactory, loggerFactory.CreateLogger<EpisodeProvider>(), libraryManager, httpContextAccessor, doubanApi, tmdbApi, omdbApi, imdbApi)
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
            // 覆盖所有元数据：info的Name、IndexNumber和ParentIndexNumber是从文件名解析出来的，provinceIds保留所有旧值
            // 搜索缺少的元数据：info的Name、IndexNumber和ParentIndexNumber是从当前的元数据获取，provinceIds保留所有旧值
            this.Log($"GetEpisodeMetadata of [name]: {info.Name} number: {info.IndexNumber} ParentIndexNumber: {info.ParentIndexNumber} IsAutomated: {info.IsAutomated}");
            var result = new MetadataResult<Episode>();

            // 动画特典和extras处理
            var specialResult = this.HandleAnimeExtras(info);
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
            if (seasonResult == null || seasonResult.Episodes == null || seasonResult.Episodes.Count < episodeNumber.Value)
            {
                this.Log("Can‘t found episode data from tmdb. Name: {0} seriesTmdbId: {1} seasonNumber: {2} episodeNumber: {3}", info.Name, seriesTmdbId, seasonNumber, episodeNumber);
                return result;
            }

            // TODO：自动搜索匹配或识别时，判断tmdb剧集信息数目和视频是否一致，不一致不处理（现在通过IsAutomated判断不太准确）
            // if (info.IsAutomated)
            // {
            //     var videoFilesCount = this.GetVideoFileCount(Path.GetDirectoryName(info.Path));
            //     if (videoFilesCount > 0 && seasonResult.Episodes.Count != videoFilesCount)
            //     {
            //         this.Log("Tmdb episode number not match. Name: {0} tmdb episode count: {1} video files count: {2}", info.Name, seasonResult.Episodes.Count, videoFilesCount);
            //         return result;
            //     }
            // }

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

        /// <summary>
        /// 重新解析文件名
        /// 注意：这里修改替换ParentIndexNumber值后，会重新触发SeasonProvier的GetMetadata方法，并带上最新的季数IndexNumber
        /// </summary>
        public EpisodeInfo FixParseInfo(EpisodeInfo info)
        {
            // 使用AnitomySharp进行重新解析，解决anime识别错误
            var fileName = Path.GetFileNameWithoutExtension(info.Path) ?? info.Name;
            var parseResult = NameParser.Parse(fileName);
            info.Year = parseResult.Year;
            info.Name = parseResult.ChineseName ?? parseResult.Name;

            // 修正文件名有特殊命名SXXEPXX时，默认解析到错误季数的问题，如神探狄仁杰 Detective.Dee.S01EP01.2006.2160p.WEB-DL.x264.AAC-HQC
            // TODO: 会导致覆盖用户手动修改元数据的季数
            if (info.ParentIndexNumber.HasValue && parseResult.ParentIndexNumber.HasValue && parseResult.ParentIndexNumber > 0 && info.ParentIndexNumber != parseResult.ParentIndexNumber)
            {
                this.Log("FixSeasonNumber by anitomy. old: {0} new: {1}", info.ParentIndexNumber, parseResult.ParentIndexNumber);
                info.ParentIndexNumber = parseResult.ParentIndexNumber;
            }

            // 没有season级目录(即虚拟季)ParentIndexNumber默认是1，季文件夹命名不规范时，ParentIndexNumber默认是null
            if (info.ParentIndexNumber is null)
            {
                info.ParentIndexNumber = parseResult.ParentIndexNumber;
            }

            // 修正anime命名格式导致的seasonNumber错误（从season元数据读取)
            if (info.ParentIndexNumber is null)
            {
                var episodeItem = _libraryManager.FindByPath(info.Path, false);
                var season = episodeItem != null ? ((Episode)episodeItem).Season : null;
                if (season != null && season.IndexNumber.HasValue && info.ParentIndexNumber != season.IndexNumber)
                {
                    this.Log("FixSeasonNumber by season. old: {0} new: {1}", info.ParentIndexNumber, season.IndexNumber);
                    info.ParentIndexNumber = season.IndexNumber;
                }

                // // 当没有season级目录时，默认为1，即当成只有一季（不需要处理，虚拟季jellyfin默认传的ParentIndexNumber=1）
                // if (info.ParentIndexNumber is null && season != null && season.LocationType == LocationType.Virtual)
                // {
                //     this.Log("FixSeasonNumber: season is virtual, set to default 1");
                //     info.ParentIndexNumber = 1;
                // }
            }

            // 从季文件夹名称猜出season number
            var seasonFolderPath = Path.GetDirectoryName(info.Path);
            if (info.ParentIndexNumber is null && seasonFolderPath != null)
            {
                info.ParentIndexNumber = this.GuessSeasonNumberByDirectoryName(seasonFolderPath);
            }

            // 识别特典
            if (info.ParentIndexNumber is null && NameParser.IsAnime(fileName) && (parseResult.IsSpecial || NameParser.IsSpecialDirectory(info.Path)))
            {
                info.ParentIndexNumber = 0;
            }

            // // 设为默认季数为1（问题：当同时存在S01和剧场版季文件夹时，剧场版的影片会因为默认第一季而在S01也显示出来）
            // if (info.ParentIndexNumber is null)
            // {
            //     this.Log("FixSeasonNumber: season number is null, set to default 1");
            //     info.ParentIndexNumber = 1;
            // }

            // 特典优先使用文件名（特典除了前面特别设置，还有SXX/Season XX等默认的）
            if (info.ParentIndexNumber.HasValue && info.ParentIndexNumber == 0)
            {
                info.Name = parseResult.SpecialName == info.Name ? fileName : parseResult.SpecialName;
            }

            // 大于1000，可能错误解析了分辨率
            if (parseResult.IndexNumber.HasValue && parseResult.IndexNumber < 1000 && info.IndexNumber != parseResult.IndexNumber)
            {
                this.Log("FixEpisodeNumber by anitomy. old: {0} new: {1}", info.IndexNumber, parseResult.IndexNumber);
                info.IndexNumber = parseResult.IndexNumber;
            }

            this.Log("FixParseInfo: fileName: {0} seasonNumber: {1} episodeNumber: {2} name: {3}", fileName, info.ParentIndexNumber, info.IndexNumber, info.Name);
            return info;
        }


        private MetadataResult<Episode>? HandleAnimeExtras(EpisodeInfo info)
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
                    };
                    return result;
                }

                // 没ParentIndexNumber时只修改名称
                result.Item = new Episode
                {
                    Name = parseResult.ExtraName,
                };
                return result;
            }

            //// 特典也有剧集信息，不在这里处理
            // if (parseResult.IsSpecial || NameParser.IsSpecialDirectory(info.Path))
            // {
            //     this.Log($"Found anime sp of [name]: {fileName}");
            //     var result = new MetadataResult<Episode>();
            //     result.HasMetadata = true;
            //     result.Item = new Episode
            //     {
            //         ParentIndexNumber = 0,
            //         IndexNumber = parseResult.IndexNumber,
            //         Name = parseResult.SpecialName == info.Name ? fileName : parseResult.SpecialName,
            //     };

            //     return result;
            // }

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
