using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using AngleSharp.Text;
using Jellyfin.Plugin.MetaShark.Api;
using Jellyfin.Plugin.MetaShark.Core;
using Jellyfin.Plugin.MetaShark.Model;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using StringMetric;
using TMDbLib.Client;
using TMDbLib.Objects.Find;
using TMDbLib.Objects.Languages;
using TMDbLib.Objects.TvShows;

namespace Jellyfin.Plugin.MetaShark.Providers
{
    public class MovieProvider : BaseProvider, IRemoteMetadataProvider<Movie, MovieInfo>
    {
        public MovieProvider(IHttpClientFactory httpClientFactory, ILoggerFactory loggerFactory, ILibraryManager libraryManager, IHttpContextAccessor httpContextAccessor, DoubanApi doubanApi, TmdbApi tmdbApi, OmdbApi omdbApi)
            : base(httpClientFactory, loggerFactory.CreateLogger<MovieProvider>(), libraryManager, httpContextAccessor, doubanApi, tmdbApi, omdbApi)
        {
        }

        public string Name => Plugin.PluginName;


        /// <inheritdoc />
        public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(MovieInfo info, CancellationToken cancellationToken)
        {
            this.Log($"GetSearchResults of [name]: {info.Name}");
            var result = new List<RemoteSearchResult>();
            if (string.IsNullOrEmpty(info.Name))
            {
                return result;
            }

            // 从douban搜索
            var res = await this._doubanApi.SearchAsync(info.Name, cancellationToken).ConfigureAwait(false);
            result.AddRange(res.Take(this.config.MaxSearchResult).Select(x =>
            {
                return new RemoteSearchResult
                {
                    SearchProviderName = DoubanProviderName,
                    ProviderIds = new Dictionary<string, string> { { DoubanProviderId, x.Sid } },
                    ImageUrl = this.GetProxyImageUrl(x.Img),
                    ProductionYear = x.Year,
                    Name = x.Name,
                };
            }));


            // 从tmdb搜索
            if (this.config.EnableTmdbSearch)
            {
                var tmdbList = await _tmdbApi.SearchMovieAsync(info.Name, info.MetadataLanguage, cancellationToken).ConfigureAwait(false);
                result.AddRange(tmdbList.Take(this.config.MaxSearchResult).Select(x =>
                {
                    return new RemoteSearchResult
                    {
                        SearchProviderName = TmdbProviderName,
                        ProviderIds = new Dictionary<string, string> { { MetadataProvider.Tmdb.ToString(), x.Id.ToString(CultureInfo.InvariantCulture) } },
                        Name = string.Format("[TMDB]{0}", x.Title ?? x.OriginalTitle),
                        ImageUrl = this._tmdbApi.GetPosterUrl(x.PosterPath),
                        Overview = x.Overview,
                        ProductionYear = x.ReleaseDate?.Year,
                    };
                }));
            }

            return result;
        }

        /// <inheritdoc />
        public async Task<MetadataResult<Movie>> GetMetadata(MovieInfo info, CancellationToken cancellationToken)
        {
            this.Log($"GetMovieMetadata of [name]: {info.Name}");
            var result = new MetadataResult<Movie>();

            // 使用刷新元数据时，providerIds会保留旧有值，只有识别/新增才会没值
            var sid = info.GetProviderId(DoubanProviderId);
            var tmdbId = info.GetProviderId(MetadataProvider.Tmdb);
            var metaSource = info.GetProviderId(Plugin.ProviderId);
            // 注意：会存在元数据有tmdbId，但metaSource没值的情况（之前由TMDB插件刮削导致）
            var hasTmdbMeta = metaSource == MetaSource.Tmdb && !string.IsNullOrEmpty(tmdbId);
            var hasDoubanMeta = metaSource != MetaSource.Tmdb && !string.IsNullOrEmpty(sid);
            if (!hasDoubanMeta && !hasTmdbMeta)
            {
                // 自动扫描搜索匹配元数据
                sid = await this.GuessByDoubanAsync(info, cancellationToken).ConfigureAwait(false);
                // if (string.IsNullOrEmpty(sid))
                // {
                //     tmdbId = await this.GuestByTmdbAsync(info, cancellationToken).ConfigureAwait(false);
                // }
            }

            if (metaSource != MetaSource.Tmdb && !string.IsNullOrEmpty(sid))
            {
                this.Log($"GetMovieMetadata of douban [sid]: \"{sid}\"");
                var subject = await this._doubanApi.GetMovieAsync(sid, cancellationToken).ConfigureAwait(false);
                if (subject == null)
                {
                    return result;
                }
                subject.Celebrities = await this._doubanApi.GetCelebritiesBySidAsync(sid, cancellationToken).ConfigureAwait(false);

                var movie = new Movie
                {
                    ProviderIds = new Dictionary<string, string> { { DoubanProviderId, subject.Sid }, { Plugin.ProviderId, MetaSource.Douban } },
                    Name = subject.Name,
                    OriginalTitle = subject.OriginalName,
                    CommunityRating = subject.Rating,
                    Overview = subject.Intro,
                    ProductionYear = subject.Year,
                    HomePageUrl = "https://www.douban.com",
                    Genres = subject.Genres,
                    // ProductionLocations = [x?.Country],
                    PremiereDate = subject.ScreenTime,
                    Tagline = string.Empty,
                };
                if (!string.IsNullOrEmpty(subject.Imdb))
                {
                    movie.SetProviderId(MetadataProvider.Imdb, subject.Imdb);

                    // 通过imdb获取TMDB id
                    var newTmdbId = await this.GetTmdbIdByImdbAsync(subject.Imdb, info.MetadataLanguage, cancellationToken).ConfigureAwait(false);
                    if (!string.IsNullOrEmpty(newTmdbId))
                    {
                        tmdbId = newTmdbId;
                        movie.SetProviderId(MetadataProvider.Tmdb, tmdbId);
                    }
                }

                // 通过imdb获取电影系列信息
                if (this.config.EnableTmdbCollection && !string.IsNullOrEmpty(tmdbId))
                {
                    try
                    {
                        var movieResult = await _tmdbApi
                                        .GetMovieAsync(Convert.ToInt32(tmdbId, CultureInfo.InvariantCulture), info.MetadataLanguage, info.MetadataLanguage, cancellationToken)
                                        .ConfigureAwait(false);
                        if (movieResult != null && movieResult.BelongsToCollection != null)
                        {
                            movie.CollectionName = movieResult.BelongsToCollection.Name;
                        }
                    }
                    catch (Exception ex)
                    { }
                }


                result.Item = movie;
                result.QueriedById = true;
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


            if (!string.IsNullOrEmpty(tmdbId))
            {
                this.Log($"GetMovieMetadata of tmdb [id]: \"{tmdbId}\"");
                var movieResult = await _tmdbApi
                            .GetMovieAsync(Convert.ToInt32(tmdbId, CultureInfo.InvariantCulture), info.MetadataLanguage, info.MetadataLanguage, cancellationToken)
                            .ConfigureAwait(false);

                if (movieResult == null)
                {
                    return result;
                }

                var movie = new Movie
                {
                    Name = movieResult.Title ?? movieResult.OriginalTitle,
                    Overview = movieResult.Overview?.Replace("\n\n", "\n", StringComparison.InvariantCulture),
                    Tagline = movieResult.Tagline,
                    ProductionLocations = movieResult.ProductionCountries.Select(pc => pc.Name).ToArray()
                };
                result = new MetadataResult<Movie>
                {
                    QueriedById = true,
                    HasMetadata = true,
                    ResultLanguage = info.MetadataLanguage,
                    Item = movie
                };

                movie.SetProviderId(MetadataProvider.Tmdb, tmdbId);
                movie.SetProviderId(MetadataProvider.Imdb, movieResult.ImdbId);
                movie.SetProviderId(Plugin.ProviderId, MetaSource.Tmdb);

                // 获取电影系列信息
                if (this.config.EnableTmdbCollection && movieResult.BelongsToCollection != null)
                {
                    movie.CollectionName = movieResult.BelongsToCollection.Name;
                }

                movie.CommunityRating = (float)System.Math.Round(movieResult.VoteAverage, 2);
                movie.PremiereDate = movieResult.ReleaseDate;
                movie.ProductionYear = movieResult.ReleaseDate?.Year;

                if (movieResult.ProductionCompanies != null)
                {
                    movie.SetStudios(movieResult.ProductionCompanies.Select(c => c.Name));
                }

                var genres = movieResult.Genres;

                foreach (var genre in genres.Select(g => g.Name))
                {
                    movie.AddGenre(genre);
                }

                foreach (var person in GetPersons(movieResult))
                {
                    result.AddPerson(person);
                }

                return result;
            }

            return result;
        }


        private IEnumerable<PersonInfo> GetPersons(TMDbLib.Objects.Movies.Movie item)
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
                        personInfo.ImageUrl = _tmdbApi.GetProfileUrl(actor.ProfilePath);
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

                    if (!keepTypes.Contains(type, StringComparer.OrdinalIgnoreCase) &&
                            !keepTypes.Contains(person.Job ?? string.Empty, StringComparer.OrdinalIgnoreCase))
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

    }
}
