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
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TMDbLib.Objects.Find;
using TMDbLib.Objects.TvShows;
using System.Text.RegularExpressions;
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
                // 季文件夹名称不规范，没法拿到seasonNumber，尝试从文件名猜出
                if (seasonNumber is null)
                {
                    seasonNumber = this.GuessSeasonNumberByFileName(info.Path);
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
                            OriginalTitle = subject.OriginalName,
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
                if (string.IsNullOrEmpty(seasonSid) && !string.IsNullOrWhiteSpace(seriesTmdbId) && (seasonNumber.HasValue && seasonNumber > 0))
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
            return await this.GetMetadataByTmdb(info, seriesTmdbId, seasonNumber, cancellationToken).ConfigureAwait(false);
        }

        public int? GuessSeasonNumberByFileName(string path)
        {
            // 当没有season级目录时，path为空，直接返回
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            // TODO: 有时series name中会带有季信息
            var fileName = Path.GetFileName(path);
            if (string.IsNullOrEmpty(fileName))
            {
                return null;
            }

            var regSeason = new Regex(@"第(.)(季|部)", RegexOptions.Compiled);
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
                {@"[ ._](I|1st)[ ._]", 1},
                {@"[ ._](II|2nd)[ ._]", 2},
                {@"[ ._](III|3rd)[ ._]", 3},
                {@"[ ._](IIII|4th)[ ._]", 3},
            };

            foreach (var entry in seasonNameMap)
            {
                if (Regex.IsMatch(fileName, entry.Key))
                {
                    this.Log($"Found season number of filename: {fileName} seasonNumber: {entry.Value}");
                    return entry.Value;
                }
            }

            // 带数字末尾的
            match = Regex.Match(fileName, @"[ ._](\d{1,2})$");
            if (match.Success && match.Groups.Count > 1)
            {
                var seasonNumber = match.Groups[1].Value.ToInt();
                if (seasonNumber > 0)
                {
                    this.Log($"Found season number of filename: {fileName} seasonNumber: {seasonNumber}");
                    return seasonNumber;
                }
            }

            return null;
        }

        public async Task<string?> GuessDoubanSeasonId(string? sid, string? seriesTmdbId, int? seasonNumber, ItemLookupInfo info, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(sid))
            {
                return null;
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
                return await this.GuestDoubanSeasonByYearAsync(seriesName, seasonYear, cancellationToken).ConfigureAwait(false);
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
            foreach (var person in GetPersons(seasonResult))
            {
                result.AddPerson(person);
            }

            return result;
        }


        private IEnumerable<PersonInfo> GetPersons(TvSeason item)
        {
            // 演员
            if (item.Credits?.Cast != null)
            {
                foreach (var actor in item.Credits.Cast.OrderBy(a => a.Order).Take(this.config.MaxCastMembers))
                {
                    var personInfo = new PersonInfo
                    {
                        Name = actor.Name.Trim(),
                        Role = actor.Character,
                        Type = PersonType.Actor,
                        SortOrder = actor.Order,
                    };

                    if (!string.IsNullOrWhiteSpace(actor.ProfilePath))
                    {
                        personInfo.ImageUrl = this._tmdbApi.GetProfileUrl(actor.ProfilePath);
                    }

                    if (actor.Id > 0)
                    {
                        personInfo.SetProviderId(MetadataProvider.Tmdb, actor.Id.ToString(CultureInfo.InvariantCulture));
                    }


                    yield return personInfo;
                }
            }

            // 导演
            if (item.Credits?.Crew != null)
            {
                var keepTypes = new[]
                {
                    PersonType.Director,
                    PersonType.Writer,
                    PersonType.Producer
                };

                foreach (var person in item.Credits.Crew)
                {
                    // Normalize this
                    var type = MapCrewToPersonType(person);

                    if (!keepTypes.Contains(type, StringComparer.OrdinalIgnoreCase)
                        && !keepTypes.Contains(person.Job ?? string.Empty, StringComparer.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var personInfo = new PersonInfo
                    {
                        Name = person.Name.Trim(),
                        Role = person.Job,
                        Type = type
                    };

                    if (!string.IsNullOrWhiteSpace(person.ProfilePath))
                    {
                        personInfo.ImageUrl = this._tmdbApi.GetPosterUrl(person.ProfilePath);
                    }

                    if (person.Id > 0)
                    {
                        personInfo.SetProviderId(MetadataProvider.Tmdb, person.Id.ToString(CultureInfo.InvariantCulture));
                    }

                    yield return personInfo;
                }
            }

        }

        /// <inheritdoc />
        public async Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            this.Log("GetImageResponse url: {0}", url);
            return await this._httpClientFactory.CreateClient().GetAsync(new Uri(url), cancellationToken).ConfigureAwait(false);
        }

        private void Log(string? message, params object?[] args)
        {
            this._logger.LogInformation($"[MetaShark] {message}", args);
        }
    }
}
