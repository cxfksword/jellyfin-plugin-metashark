using Jellyfin.Plugin.MetaShark.Api;
using Jellyfin.Plugin.MetaShark.Core;
using Jellyfin.Plugin.MetaShark.Model;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Jellyfin.Plugin.MetaShark.Providers
{
    public class SeasonProvider : BaseProvider, IRemoteMetadataProvider<Season, SeasonInfo>
    {

        public SeasonProvider(IHttpClientFactory httpClientFactory, ILoggerFactory loggerFactory, ILibraryManager libraryManager, IHttpContextAccessor httpContextAccessor, DoubanApi doubanApi, TmdbApi tmdbApi, OmdbApi omdbApi)
            : base(httpClientFactory, loggerFactory.CreateLogger<SeasonProvider>(), libraryManager, httpContextAccessor, doubanApi, tmdbApi, omdbApi)
        {
        }

        public string Name => Plugin.PluginName;


        /// <inheritdoc />
        public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(SeasonInfo info, CancellationToken cancellationToken)
        {
            this.Log($"GetSeasonSearchResults of [name]: {info.Name}");
            return await Task.FromResult(Enumerable.Empty<RemoteSearchResult>());
        }

        /// <inheritdoc />
        public async Task<MetadataResult<Season>> GetMetadata(SeasonInfo info, CancellationToken cancellationToken)
        {
            var result = new MetadataResult<Season>();

            info.SeriesProviderIds.TryGetValue(MetadataProvider.Tmdb.ToString(), out var seriesTmdbId);
            info.SeriesProviderIds.TryGetValue(Plugin.ProviderId, out var metaSource);
            info.SeriesProviderIds.TryGetValue(DoubanProviderId, out var sid);
            var seasonNumber = info.IndexNumber; // S00/Season 00特典目录会为0
            var seasonSid = info.GetProviderId(DoubanProviderId);
            this.Log($"GetSeasonMetaData of [name]: {info.Name}  number: {info.IndexNumber} seriesTmdbId: {seriesTmdbId} sid: {sid} metaSource: {metaSource} IsAutomated: {info.IsAutomated}");

            if (metaSource != MetaSource.Tmdb && !string.IsNullOrEmpty(sid))
            {
                // 季文件夹名称不规范，没法拿到seasonNumber，尝试从文件夹名猜出
                // 注意：本办法没法处理没有季文件夹的/虚拟季，因为path会为空
                if (seasonNumber is null)
                {
                    seasonNumber = this.GuessSeasonNumberByDirectoryName(info.Path);
                }

                // 搜索豆瓣季id
                if (string.IsNullOrEmpty(seasonSid))
                {
                    seasonSid = await this.GuessDoubanSeasonId(sid, seriesTmdbId, seasonNumber, info, cancellationToken).ConfigureAwait(false);
                }

                // 获取季豆瓣数据
                if (!string.IsNullOrEmpty(seasonSid))
                {
                    var subject = await this._doubanApi.GetMovieAsync(seasonSid, cancellationToken).ConfigureAwait(false);
                    if (subject != null)
                    {
                        subject.Celebrities = await this._doubanApi.GetCelebritiesBySidAsync(seasonSid, cancellationToken).ConfigureAwait(false);

                        var movie = new Season
                        {
                            ProviderIds = new Dictionary<string, string> { { DoubanProviderId, subject.Sid } },
                            Name = subject.Name,
                            CommunityRating = subject.Rating,
                            Overview = subject.Intro,
                            ProductionYear = subject.Year,
                            Genres = subject.Genres,
                            PremiereDate = subject.ScreenTime,  // 发行日期
                            IndexNumber = seasonNumber,
                        };

                        result.Item = movie;
                        result.HasMetadata = true;
                        subject.LimitDirectorCelebrities.Take(this.config.MaxCastMembers).ToList().ForEach(c => result.AddPerson(new PersonInfo
                        {
                            Name = c.Name,
                            Type = c.RoleType,
                            Role = c.Role,
                            ImageUrl = c.Img,
                            ProviderIds = new Dictionary<string, string> { { DoubanProviderId, c.Id } },
                        }));

                        return result;
                    }
                }
                else
                {
                    this.Log($"GetSeasonMetaData of [name]: {info.Name} not found douban season id!");
                }


                // 豆瓣找不到季数据，尝试获取tmdb的季数据
                if (string.IsNullOrEmpty(seasonSid) && !string.IsNullOrWhiteSpace(seriesTmdbId) && seasonNumber.HasValue && seasonNumber >= 0)
                {
                    var tmdbResult = await this.GetMetadataByTmdb(info, seriesTmdbId, seasonNumber.Value, cancellationToken).ConfigureAwait(false);
                    if (tmdbResult != null)
                    {
                        return tmdbResult;
                    }
                }


                // 从豆瓣获取不到季信息
                return result;
            }


            // series使用TMDB元数据来源
            // tmdb季级没有对应id，只通过indexNumber区分
            if (metaSource == MetaSource.Tmdb && !string.IsNullOrEmpty(seriesTmdbId))
            {
                return await this.GetMetadataByTmdb(info, seriesTmdbId, seasonNumber, cancellationToken).ConfigureAwait(false);
            }

            return result;
        }


        public async Task<string?> GuessDoubanSeasonId(string? sid, string? seriesTmdbId, int? seasonNumber, ItemLookupInfo info, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(sid))
            {
                return null;
            }

            // 没有季文件夹（即虚拟季），info.Path会为空，直接用series的sid
            if (string.IsNullOrEmpty(info.Path))
            {
                return sid;
            }

            // 从sereis获取正确名称，info.Name当是标准格式如S01等时，会变成第x季，非标准名称默认文件名
            var series = await this._doubanApi.GetMovieAsync(sid, cancellationToken).ConfigureAwait(false);
            if (series == null)
            {
                return null;
            }
            var seriesName = RemoveSeasonSubfix(series.Name);

            // 没有季id，但存在tmdbid，尝试从tmdb获取对应季的年份信息，用于从豆瓣搜索对应季数据
            var seasonYear = 0;
            if (!string.IsNullOrEmpty(seriesTmdbId) && (seasonNumber.HasValue && seasonNumber > 0))
            {
                var season = await this._tmdbApi
                    .GetSeasonAsync(seriesTmdbId.ToInt(), seasonNumber.Value, info.MetadataLanguage, info.MetadataLanguage, cancellationToken)
                    .ConfigureAwait(false);
                seasonYear = season?.AirDate?.Year ?? 0;
            }

            if (!string.IsNullOrEmpty(seriesName) && seasonYear > 0)
            {
                var seasonSid = await this.GuestDoubanSeasonByYearAsync(seriesName, seasonYear, seasonNumber, cancellationToken).ConfigureAwait(false);
                if (!string.IsNullOrEmpty(seasonSid))
                {
                    return seasonSid;
                }
            }

            // 通过季名匹配douban id，作为关闭tmdb api/api超时的后备方法使用
            if (!string.IsNullOrEmpty(seriesName) && seasonNumber.HasValue && seasonNumber > 0)
            {
                return await this.GuestDoubanSeasonBySeasonNameAsync(seriesName, seasonNumber, cancellationToken).ConfigureAwait(false);
            }

            return null;
        }

        public async Task<MetadataResult<Season>> GetMetadataByTmdb(SeasonInfo info, string? seriesTmdbId, int? seasonNumber, CancellationToken cancellationToken)
        {
            var result = new MetadataResult<Season>();

            if (string.IsNullOrEmpty(seriesTmdbId))
            {
                return result;
            }

            if (seasonNumber is null or 0)
            {
                return result;
            }

            var seasonResult = await this._tmdbApi
                .GetSeasonAsync(seriesTmdbId.ToInt(), seasonNumber ?? 0, info.MetadataLanguage, info.MetadataLanguage, cancellationToken)
                .ConfigureAwait(false);
            if (seasonResult == null)
            {
                this.Log($"Not found season from TMDB. {info.Name} seriesTmdbId: {seriesTmdbId} seasonNumber: {seasonNumber}");
                return result;
            }


            result.HasMetadata = true;
            result.Item = new Season
            {
                Name = seasonResult.Name,
                IndexNumber = seasonNumber,
                Overview = seasonResult.Overview,
                PremiereDate = seasonResult.AirDate,
                ProductionYear = seasonResult.AirDate?.Year,
            };

            if (!string.IsNullOrEmpty(seasonResult.ExternalIds?.TvdbId))
            {
                result.Item.SetProviderId(MetadataProvider.Tvdb, seasonResult.ExternalIds.TvdbId);
            }

            return result;
        }

    }
}
