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

namespace Jellyfin.Plugin.MetaShark.Providers
{
    public class SeasonProvider : BaseProvider, IRemoteMetadataProvider<Season, SeasonInfo>
    {

        public SeasonProvider(IHttpClientFactory httpClientFactory, ILoggerFactory loggerFactory, ILibraryManager libraryManager, DoubanApi doubanApi, TmdbApi tmdbApi, OmdbApi omdbApi)
            : base(httpClientFactory, loggerFactory.CreateLogger<SeasonProvider>(), libraryManager, doubanApi, tmdbApi, omdbApi)
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
            var seasonNumber = info.IndexNumber;
            var seasonSid = info.GetProviderId(DoubanProviderId);
            this.Log($"GetSeasonMetaData of [name]: {info.Name}  number: {info.IndexNumber} seriesTmdbId: {seriesTmdbId} sid: {sid} metaSource: {metaSource}");

            if (metaSource != MetaSource.Tmdb && !string.IsNullOrEmpty(sid))
            {
                // 从sereis获取正确名称，季名称有时不对
                var series = await this._doubanApi.GetMovieAsync(sid, cancellationToken).ConfigureAwait(false);
                if (series == null)
                {
                    return result;
                }
                var seriesName = series.Name;

                // 存在tmdbid，尝试从tmdb获取对应季的年份信息，用于从豆瓣搜索对应季数据
                if (string.IsNullOrEmpty(seasonSid))
                {
                    var seasonYear = 0;
                    if (!string.IsNullOrEmpty(seriesTmdbId) && seasonNumber.HasValue)
                    {
                        var season = await this._tmdbApi
                            .GetSeasonAsync(seriesTmdbId.ToInt(), seasonNumber.Value, info.MetadataLanguage, info.MetadataLanguage, cancellationToken)
                            .ConfigureAwait(false);
                        seasonYear = season?.AirDate?.Year ?? 0;
                    }

                    if (!string.IsNullOrEmpty(seriesName) && seasonYear > 0)
                    {
                        seasonSid = await this.GuestSeasonByDoubanAsync(seriesName, seasonYear, cancellationToken).ConfigureAwait(false);
                    }
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
                            PremiereDate = subject.ScreenTime,
                            IndexNumber = info.IndexNumber,
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


                // 从豆瓣获取不到季信息，直接使用series信息
                result.Item = new Season
                {
                    ProviderIds = new Dictionary<string, string> { { DoubanProviderId, sid } },
                    Name = series.Name,
                    OriginalTitle = series.OriginalName,
                    CommunityRating = series.Rating,
                    Overview = series.Intro,
                    ProductionYear = series.Year,
                    Genres = series.Genres,
                    PremiereDate = series.ScreenTime,
                };

                result.QueriedById = true;
                result.HasMetadata = true;
                return result;
            }


            // series使用TMDB元数据来源
            // tmdb季级没有对应id，只通过indexNumber区分
            if (!string.IsNullOrWhiteSpace(seriesTmdbId) && seasonNumber.HasValue)
            {
                var seasonResult = await this._tmdbApi
                .GetSeasonAsync(seriesTmdbId.ToInt(), seasonNumber.Value, info.MetadataLanguage, null, cancellationToken)
                .ConfigureAwait(false);
                if (seasonResult == null)
                {
                    this.Log($"Not found season from TMDB. {info.Name} seriesTmdbId: {seriesTmdbId} seasonNumber: {seasonNumber}");
                    return result;
                }

                result.HasMetadata = true;
                result.Item = new Season
                {
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


            // 季手工修正（先手工修改元数据，再刷新元数据->覆盖所有元数据），通过季名称重新搜索
            // var guessName = Regex.Replace(info.Name, Pattern, " ");
            // this.Log($"Try search season by name. original name: {info.Name} guess name: {guessName}");
            // var guessSid = await this.GuestByDoubanAsync(info, cancellationToken).ConfigureAwait(false);
            // if (!string.IsNullOrEmpty(guessSid))
            // {

            //     var subject = await this._doubanApi.GetMovieAsync(guessSid, cancellationToken).ConfigureAwait(false);
            //     if (subject != null)
            //     {
            //         subject.Celebrities = await this._doubanApi.GetCelebritiesBySidAsync(guessSid, cancellationToken).ConfigureAwait(false);

            //         var movie = new Season
            //         {
            //             ProviderIds = new Dictionary<string, string> { { DoubanProviderId, subject.Sid } },
            //             Name = subject.Name,
            //             OriginalTitle = subject.OriginalName,
            //             CommunityRating = subject.Rating,
            //             Overview = subject.Intro,
            //             ProductionYear = subject.Year,
            //             Genres = subject.Genres,
            //             PremiereDate = subject.ScreenTime,
            //             IndexNumber = info.IndexNumber,
            //         };

            //         result.Item = movie;
            //         result.HasMetadata = true;
            //         subject.Celebrities.Take(this.config.MaxCastMembers).ToList().ForEach(c => result.AddPerson(new PersonInfo
            //         {
            //             Name = c.Name,
            //             Type = c.RoleType,
            //             Role = c.Role,
            //             ImageUrl = c.Img,
            //             ProviderIds = new Dictionary<string, string> { { DoubanProviderId, c.Id } },
            //         }));

            //         return result;
            //     }
            // }

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
