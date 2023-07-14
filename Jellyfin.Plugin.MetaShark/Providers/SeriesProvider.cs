using Jellyfin.Plugin.MetaShark.Api;
using Jellyfin.Plugin.MetaShark.Model;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using TMDbLib.Objects.TvShows;
using MetadataProvider = MediaBrowser.Model.Entities.MetadataProvider;

namespace Jellyfin.Plugin.MetaShark.Providers
{
    public class SeriesProvider : BaseProvider, IRemoteMetadataProvider<Series, SeriesInfo>
    {
        public SeriesProvider(IHttpClientFactory httpClientFactory, ILoggerFactory loggerFactory, ILibraryManager libraryManager, IHttpContextAccessor httpContextAccessor, DoubanApi doubanApi, TmdbApi tmdbApi, OmdbApi omdbApi, ImdbApi imdbApi)
            : base(httpClientFactory, loggerFactory.CreateLogger<SeriesProvider>(), libraryManager, httpContextAccessor, doubanApi, tmdbApi, omdbApi, imdbApi)
        {
        }

        public string Name => Plugin.PluginName;


        /// <inheritdoc />
        public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(SeriesInfo info, CancellationToken cancellationToken)
        {
            this.Log($"GetSearchResults of [name]: {info.Name}");
            var result = new List<RemoteSearchResult>();
            if (string.IsNullOrEmpty(info.Name))
            {
                return result;
            }

            // 从douban搜索
            var res = await this._doubanApi.SearchAsync(info.Name, cancellationToken).ConfigureAwait(false);
            result.AddRange(res.Take(Configuration.PluginConfiguration.MAX_SEARCH_RESULT).Select(x =>
            {
                return new RemoteSearchResult
                {
                    ProviderIds = new Dictionary<string, string> { { DoubanProviderId, x.Sid } },
                    ImageUrl = this.GetProxyImageUrl(x.Img),
                    ProductionYear = x.Year,
                    Name = x.Name,
                };
            }));

            // 尝试从tmdb搜索
            if (this.config.EnableTmdbSearch)
            {
                var tmdbList = await this._tmdbApi.SearchSeriesAsync(info.Name, info.MetadataLanguage, cancellationToken).ConfigureAwait(false);
                result.AddRange(tmdbList.Take(Configuration.PluginConfiguration.MAX_SEARCH_RESULT).Select(x =>
                {
                    return new RemoteSearchResult
                    {
                        ProviderIds = new Dictionary<string, string> { { MetadataProvider.Tmdb.ToString(), x.Id.ToString(CultureInfo.InvariantCulture) } },
                        Name = string.Format("[TMDB]{0}", x.Name ?? x.OriginalName),
                        ImageUrl = this._tmdbApi.GetPosterUrl(x.PosterPath),
                        Overview = x.Overview,
                        ProductionYear = x.FirstAirDate?.Year,
                    };
                }));
            }

            return result;
        }

        /// <inheritdoc />
        public async Task<MetadataResult<Series>> GetMetadata(SeriesInfo info, CancellationToken cancellationToken)
        {
            this.Log($"GetSeriesMetadata of [name]: {info.Name} IsAutomated: {info.IsAutomated}");
            var result = new MetadataResult<Series>();

            var sid = info.GetProviderId(DoubanProviderId);
            var tmdbId = info.GetProviderId(MetadataProvider.Tmdb);
            var metaSource = info.GetProviderId(Plugin.ProviderId);
            // 用于修正识别时指定tmdb，没法读取tmdb数据的BUG。。。两个合在一起太难了。。。
            if (string.IsNullOrEmpty(metaSource) && info.Name.StartsWith("[TMDB]"))
            {
                metaSource = MetaSource.Tmdb;
            }
            // 注意：会存在元数据有tmdbId，但metaSource没值的情况（之前由TMDB插件刮削导致）
            var hasTmdbMeta = metaSource == MetaSource.Tmdb && !string.IsNullOrEmpty(tmdbId);
            var hasDoubanMeta = metaSource != MetaSource.Tmdb && !string.IsNullOrEmpty(sid);
            if (!hasDoubanMeta && !hasTmdbMeta)
            {
                // 自动扫描搜索匹配元数据
                sid = await this.GuessByDoubanAsync(info, cancellationToken).ConfigureAwait(false);
            }

            if (metaSource != MetaSource.Tmdb && !string.IsNullOrEmpty(sid))
            {
                this.Log($"GetSeriesMetadata of douban [sid]: {sid}");
                var subject = await this._doubanApi.GetMovieAsync(sid, cancellationToken).ConfigureAwait(false);
                if (subject == null)
                {
                    return result;
                }
                subject.Celebrities = await this._doubanApi.GetCelebritiesBySidAsync(sid, cancellationToken).ConfigureAwait(false);

                var seriesName = RemoveSeasonSubfix(subject.Name);
                var item = new Series
                {
                    ProviderIds = new Dictionary<string, string> { { DoubanProviderId, subject.Sid }, { Plugin.ProviderId, MetaSource.Douban } },
                    Name = seriesName,
                    OriginalTitle = RemoveSeasonSubfix(subject.OriginalName),
                    CommunityRating = subject.Rating,
                    Overview = subject.Intro,
                    ProductionYear = subject.Year,
                    HomePageUrl = "https://www.douban.com",
                    Genres = subject.Genres,
                    PremiereDate = subject.ScreenTime,
                    Tagline = string.Empty,
                };

                // 设置imdb元数据
                if (!string.IsNullOrEmpty(subject.Imdb))
                {
                    var newImdbId = await this.CheckNewImdbID(subject.Imdb, cancellationToken).ConfigureAwait(false);
                    subject.Imdb = newImdbId;
                    item.SetProviderId(MetadataProvider.Imdb, newImdbId);
                }

                // 搜索匹配tmdbId
                var newTmdbId = await this.FindTmdbId(seriesName, subject.Imdb, subject.Year, info, cancellationToken).ConfigureAwait(false);
                if (!string.IsNullOrEmpty(newTmdbId))
                {
                    tmdbId = newTmdbId;
                    item.SetProviderId(MetadataProvider.Tmdb, tmdbId);
                }

                // 通过imdb获取电影分级信息
                if (this.config.EnableTmdbOfficialRating && !string.IsNullOrEmpty(tmdbId))
                {
                    var officialRating = await this.GetTmdbOfficialRating(info, tmdbId, cancellationToken).ConfigureAwait(false);
                    if (!string.IsNullOrEmpty(officialRating))
                    {
                        item.OfficialRating = officialRating;
                    }
                }


                result.Item = item;
                result.QueriedById = true;
                result.HasMetadata = true;
                subject.LimitDirectorCelebrities.Take(this.config.MaxCastMembers).ToList().ForEach(c => result.AddPerson(new PersonInfo
                {
                    Name = c.Name,
                    Type = c.RoleType,
                    Role = c.Role,
                    ImageUrl = this.GetLocalProxyImageUrl(c.Img),
                    ProviderIds = new Dictionary<string, string> { { DoubanProviderId, c.Id } },
                }));

                return result;
            }

            if (metaSource == MetaSource.Tmdb && !string.IsNullOrEmpty(tmdbId))
            {
                return await this.GetMetadataByTmdb(tmdbId, info, cancellationToken).ConfigureAwait(false);
            }

            this.Log($"匹配失败！可检查下年份是否与豆瓣一致，是否需要登录访问. [name]: {info.Name} [year]: {info.Year}");
            return result;
        }

        private async Task<MetadataResult<Series>> GetMetadataByTmdb(string? tmdbId, ItemLookupInfo info, CancellationToken cancellationToken)
        {
            var result = new MetadataResult<Series>();
            if (string.IsNullOrEmpty(tmdbId))
            {
                return result;
            }

            this.Log($"GetSeriesMetadata of tmdb [id]: \"{tmdbId}\"");
            var tvShow = await _tmdbApi
                .GetSeriesAsync(Convert.ToInt32(tmdbId, CultureInfo.InvariantCulture), info.MetadataLanguage, info.MetadataLanguage, cancellationToken)
                .ConfigureAwait(false);

            if (tvShow == null)
            {
                return result;
            }

            result = new MetadataResult<Series>
            {
                Item = MapTvShowToSeries(tvShow, info.MetadataCountryCode),
                ResultLanguage = info.MetadataLanguage ?? tvShow.OriginalLanguage
            };

            foreach (var person in GetPersons(tvShow))
            {
                result.AddPerson(person);
            }

            result.QueriedById = true;
            result.HasMetadata = true;
            return result;
        }

        private async Task<string?> FindTmdbId(string name, string imdb, int? year, ItemLookupInfo info, CancellationToken cancellationToken)
        {
            // 通过imdb获取TMDB id
            if (!string.IsNullOrEmpty(imdb))
            {
                var tmdbId = await this.GetTmdbIdByImdbAsync(imdb, info.MetadataLanguage, info, cancellationToken).ConfigureAwait(false);
                if (!string.IsNullOrEmpty(tmdbId))
                {
                    return tmdbId;
                }
                else
                {
                    this.Log($"Can not found tmdb [id] by imdb id: \"{imdb}\"");
                }
            }

            // 尝试通过搜索匹配获取tmdbId
            if (!string.IsNullOrEmpty(name) && year != null && year > 0)
            {
                var tmdbId = await this.GuestByTmdbAsync(name, year, info, cancellationToken).ConfigureAwait(false);
                if (!string.IsNullOrEmpty(tmdbId))
                {
                    return tmdbId;
                }
            }

            return null;
        }

        private async Task<String?> GetTmdbOfficialRating(ItemLookupInfo info, string tmdbId, CancellationToken cancellationToken)
        {

            var tvShow = await _tmdbApi
                            .GetSeriesAsync(Convert.ToInt32(tmdbId, CultureInfo.InvariantCulture), info.MetadataLanguage, info.MetadataLanguage, cancellationToken)
                            .ConfigureAwait(false);
            return this.GetTmdbOfficialRatingByData(tvShow, info.MetadataCountryCode);
        }

        private String GetTmdbOfficialRatingByData(TvShow? tvShow, string preferredCountryCode)
        {
            if (tvShow != null)
            {
                var contentRatings = tvShow.ContentRatings.Results ?? new List<ContentRating>();

                var ourRelease = contentRatings.FirstOrDefault(c => string.Equals(c.Iso_3166_1, preferredCountryCode, StringComparison.OrdinalIgnoreCase));
                var usRelease = contentRatings.FirstOrDefault(c => string.Equals(c.Iso_3166_1, "US", StringComparison.OrdinalIgnoreCase));
                var minimumRelease = contentRatings.FirstOrDefault();

                if (ourRelease != null)
                {
                    return ourRelease.Rating;
                }
                else if (usRelease != null)
                {
                    return usRelease.Rating;
                }
                else if (minimumRelease != null)
                {
                    return minimumRelease.Rating;
                }
            }

            return null;
        }

        private Series MapTvShowToSeries(TvShow seriesResult, string preferredCountryCode)
        {
            var series = new Series
            {
                Name = seriesResult.Name,
                OriginalTitle = seriesResult.OriginalName
            };

            series.SetProviderId(MetadataProvider.Tmdb, seriesResult.Id.ToString(CultureInfo.InvariantCulture));

            series.CommunityRating = (float)System.Math.Round(seriesResult.VoteAverage, 2);

            series.Overview = seriesResult.Overview;

            if (seriesResult.Networks != null)
            {
                series.Studios = seriesResult.Networks.Select(i => i.Name).ToArray();
            }

            if (seriesResult.Genres != null)
            {
                series.Genres = seriesResult.Genres.Select(i => i.Name).ToArray();
            }

            if (seriesResult.Keywords?.Results != null)
            {
                for (var i = 0; i < seriesResult.Keywords.Results.Count; i++)
                {
                    series.AddTag(seriesResult.Keywords.Results[i].Name);
                }
            }
            series.HomePageUrl = seriesResult.Homepage;

            series.RunTimeTicks = seriesResult.EpisodeRunTime.Select(i => TimeSpan.FromMinutes(i).Ticks).FirstOrDefault();

            if (string.Equals(seriesResult.Status, "Ended", StringComparison.OrdinalIgnoreCase))
            {
                series.Status = SeriesStatus.Ended;
                series.EndDate = seriesResult.LastAirDate;
            }
            else
            {
                series.Status = SeriesStatus.Continuing;
            }

            series.PremiereDate = seriesResult.FirstAirDate;

            var ids = seriesResult.ExternalIds;
            if (ids != null)
            {
                if (!string.IsNullOrWhiteSpace(ids.ImdbId))
                {
                    series.SetProviderId(MetadataProvider.Imdb, ids.ImdbId);
                }

                if (!string.IsNullOrEmpty(ids.TvrageId))
                {
                    series.SetProviderId(MetadataProvider.TvRage, ids.TvrageId);
                }

                if (!string.IsNullOrEmpty(ids.TvdbId))
                {
                    series.SetProviderId(MetadataProvider.Tvdb, ids.TvdbId);
                }
            }
            series.SetProviderId(Plugin.ProviderId, MetaSource.Tmdb);
            series.OfficialRating = this.GetTmdbOfficialRatingByData(seriesResult, preferredCountryCode);


            return series;
        }

        private IEnumerable<PersonInfo> GetPersons(TvShow seriesResult)
        {
            // 演员
            if (seriesResult.Credits?.Cast != null)
            {
                foreach (var actor in seriesResult.Credits.Cast.OrderBy(a => a.Order).Take(this.config.MaxCastMembers))
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
            if (seriesResult.Credits?.Crew != null)
            {
                var keepTypes = new[]
                {
                    PersonType.Director,
                    PersonType.Writer,
                    PersonType.Producer
                };

                foreach (var person in seriesResult.Credits.Crew)
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

    }
}
