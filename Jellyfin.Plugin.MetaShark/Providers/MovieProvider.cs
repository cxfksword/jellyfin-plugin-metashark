using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AngleSharp.Text;
using Jellyfin.Data.Enums;
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
using TMDbLib.Objects.Search;

namespace Jellyfin.Plugin.MetaShark.Providers
{
    public class MovieProvider : BaseProvider, IRemoteMetadataProvider<Movie, MovieInfo>
    {
        public MovieProvider(IHttpClientFactory httpClientFactory, ILoggerFactory loggerFactory, ILibraryManager libraryManager, IHttpContextAccessor httpContextAccessor, DoubanApi doubanApi, TmdbApi tmdbApi, OmdbApi omdbApi, ImdbApi imdbApi)
            : base(httpClientFactory, loggerFactory.CreateLogger<MovieProvider>(), libraryManager, httpContextAccessor, doubanApi, tmdbApi, omdbApi, imdbApi)
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
            var res = await this._doubanApi.SearchMovieAsync(info.Name, cancellationToken).ConfigureAwait(false);
            result.AddRange(res.Take(Configuration.PluginConfiguration.MAX_SEARCH_RESULT).Select(x =>
            {
                return new RemoteSearchResult
                {
                    // 注意：jellyfin 会判断电影所有 provider id 是否有相同的，有相同的值就会认为是同一影片，会被合并不返回，必须保持 provider id 的唯一性
                    // 这里 Plugin.ProviderId 的值做这么复杂，是为了保持唯一
                    ProviderIds = new Dictionary<string, string> { { DoubanProviderId, x.Sid }, { Plugin.ProviderId, $"{MetaSource.Douban}_{x.Sid}" } },
                    ImageUrl = this.GetProxyImageUrl(x.Img),
                    ProductionYear = x.Year,
                    Name = x.Name,
                };
            }));


            // 从tmdb搜索
            if (this.config.EnableTmdbSearch)
            {
                var tmdbList = await _tmdbApi.SearchMovieAsync(info.Name, info.MetadataLanguage, cancellationToken).ConfigureAwait(false);
                result.AddRange(tmdbList.Take(Configuration.PluginConfiguration.MAX_SEARCH_RESULT).Select(x =>
                {
                    return new RemoteSearchResult
                    {
                        // 注意：jellyfin 会判断电影所有 provider id 是否有相同的，有相同的值就会认为是同一影片，会被合并不返回，必须保持 provider id 的唯一性
                        // 这里 Plugin.ProviderId 的值做这么复杂，是为了保持唯一
                        ProviderIds = new Dictionary<string, string> { { MetadataProvider.Tmdb.ToString(), x.Id.ToString(CultureInfo.InvariantCulture) }, { Plugin.ProviderId, $"{MetaSource.Tmdb}_{x.Id}" } },
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
            var fileName = this.GetOriginalFileName(info);
            var result = new MetadataResult<Movie>();

            // 使用刷新元数据时，providerIds会保留旧有值，只有识别/新增才会没值
            var sid = info.GetProviderId(DoubanProviderId);
            var tmdbId = info.GetProviderId(MetadataProvider.Tmdb);
            
            // 优先从文件名属性格式获取TMDB ID，支持 {tmdb-12345} 和 [tmdbid-12345] 格式
            if (string.IsNullOrEmpty(tmdbId))
            {
                this.Log($"[Movie TMDB ID提取] 开始检查文件名: {fileName}");
                var tmdbIdFromFileName = this.regTmdbIdAttribute.FirstMatchGroup(fileName);
                if (!string.IsNullOrWhiteSpace(tmdbIdFromFileName))
                {
                    this.Log($"[Movie TMDB ID提取] ✓ 成功从文件名提取到TMDB ID: {tmdbIdFromFileName} (文件名: {fileName})");
                    tmdbId = tmdbIdFromFileName;
                }
                else
                {
                    this.Log($"[Movie TMDB ID提取] ✗ 文件名中未找到TMDB ID格式 (文件名: {fileName})");
                }
            }
            else
            {
                this.Log($"[Movie TMDB ID提取] 已存在TMDB ID: {tmdbId}，跳过文件名提取");
            }
            
            var metaSource = info.GetMetaSource(Plugin.ProviderId);
            // 注意：会存在元数据有tmdbId，但metaSource没值的情况（之前由TMDB插件刮削导致）
            var hasTmdbMeta = metaSource == MetaSource.Tmdb && !string.IsNullOrEmpty(tmdbId);
            var hasDoubanMeta = metaSource != MetaSource.Tmdb && !string.IsNullOrEmpty(sid);
            this.Log($"GetMovieMetadata of [name]: {info.Name} [fileName]: {fileName} metaSource: {metaSource} EnableTmdb: {config.EnableTmdb}");
            if (!hasDoubanMeta && !hasTmdbMeta)
            {
                // 处理extras影片
                var extraResult = this.HandleExtraType(info);
                if (extraResult != null)
                {
                    return extraResult;
                }

                // 自动扫描搜索匹配元数据
                sid = await this.GuessByDoubanAsync(info, cancellationToken).ConfigureAwait(false);
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
                    // 这里 Plugin.ProviderId 的值做这么复杂，是为了保持唯一
                    ProviderIds = new Dictionary<string, string> { { DoubanProviderId, subject.Sid }, { Plugin.ProviderId, $"{MetaSource.Douban}_{subject.Sid}" } },
                    Name = subject.Name,
                    OriginalTitle = subject.OriginalName,
                    CommunityRating = subject.Rating,
                    Overview = subject.Intro,
                    ProductionYear = subject.Year,
                    HomePageUrl = "https://www.douban.com",
                    Genres = subject.Genres,
                    PremiereDate = subject.ScreenTime,
                };
                if (!string.IsNullOrEmpty(subject.Imdb))
                {
                    var newImdbId = await this.CheckNewImdbID(subject.Imdb, cancellationToken).ConfigureAwait(false);
                    subject.Imdb = newImdbId;
                    movie.SetProviderId(MetadataProvider.Imdb, newImdbId);

                    // 通过imdb获取TMDB id
                    var newTmdbId = await this.GetTmdbIdByImdbAsync(subject.Imdb, info.MetadataLanguage, info, cancellationToken).ConfigureAwait(false);
                    if (!string.IsNullOrEmpty(newTmdbId))
                    {
                        tmdbId = newTmdbId;
                        movie.SetProviderId(MetadataProvider.Tmdb, tmdbId);
                    }
                }

                // 尝试通过搜索匹配获取tmdbId
                if (string.IsNullOrEmpty(tmdbId) && subject.Year > 0)
                {
                    var newTmdbId = await this.GuestByTmdbAsync(subject.Name, subject.Year, info, cancellationToken).ConfigureAwait(false);
                    if (!string.IsNullOrEmpty(newTmdbId))
                    {
                        tmdbId = newTmdbId;
                        movie.SetProviderId(MetadataProvider.Tmdb, tmdbId);
                    }
                }

                // 通过imdb获取电影系列信息
                if (!string.IsNullOrEmpty(tmdbId))
                {
                    var belongCollection = await this.GetTmdbCollection(info, tmdbId, cancellationToken).ConfigureAwait(false);
                    if (belongCollection != null && !string.IsNullOrEmpty(belongCollection.Name))
                    {
                        movie.CollectionName = belongCollection.Name;
                    }
                }

                // 通过imdb获取电影分级信息
                if (this.config.EnableTmdbOfficialRating && !string.IsNullOrEmpty(tmdbId))
                {
                    var officialRating = await this.GetTmdbOfficialRating(info, tmdbId, cancellationToken).ConfigureAwait(false);
                    if (!string.IsNullOrEmpty(officialRating))
                    {
                        movie.OfficialRating = officialRating;
                    }
                }


                result.Item = movie;
                result.QueriedById = true;
                result.HasMetadata = true;
                subject.LimitDirectorCelebrities.Take(Configuration.PluginConfiguration.MAX_CAST_MEMBERS).ToList().ForEach(c => result.AddPerson(new PersonInfo
                {
                    Name = c.Name,
                    Type = c.RoleType == PersonType.Director ? PersonKind.Director : PersonKind.Actor,
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

        private async Task<MetadataResult<Movie>> GetMetadataByTmdb(string tmdbId, MovieInfo info, CancellationToken cancellationToken)
        {
            this.Log($"GetMovieMetadata of tmdb [id]: \"{tmdbId}\"");
            var result = new MetadataResult<Movie>();
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
                OriginalTitle = movieResult.OriginalTitle,
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
            // 这里 Plugin.ProviderId 的值做这么复杂，是为了保持唯一
            movie.SetProviderId(Plugin.ProviderId, $"{MetaSource.Tmdb}_{tmdbId}");

            // 获取电影系列信息
            if (movieResult.BelongsToCollection != null)
            {
                movie.CollectionName = movieResult.BelongsToCollection.Name;
            }

            movie.CommunityRating = (float)System.Math.Round(movieResult.VoteAverage, 2);
            movie.OfficialRating = this.GetTmdbOfficialRatingByData(movieResult, info.MetadataCountryCode);
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


        private MetadataResult<Movie>? HandleExtraType(MovieInfo info)
        {
            // 特典或extra视频可能和正片放在同一目录
            // TODO：插件暂时不支持设置影片为extra类型，只能直接忽略处理（最好放extras目录）
            var fileName = Path.GetFileNameWithoutExtension(info.Path) ?? info.Name;
            var parseResult = NameParser.Parse(fileName);
            if (parseResult.IsExtra)
            {
                this.Log($"Found extra of [name]: {fileName}");
                return new MetadataResult<Movie>();
            }

            // 动画常用特典文件夹
            if (NameParser.IsSpecialDirectory(info.Path) || NameParser.IsExtraDirectory(info.Path))
            {
                this.Log($"Found extra of [name]: {fileName}");
                return new MetadataResult<Movie>();
            }

            return null;
        }


        private IEnumerable<PersonInfo> GetPersons(TMDbLib.Objects.Movies.Movie item)
        {
            // 演员
            if (item.Credits?.Cast != null)
            {
                foreach (var actor in item.Credits.Cast.OrderBy(a => a.Order).Take(Configuration.PluginConfiguration.MAX_CAST_MEMBERS))
                {
                    var personInfo = new PersonInfo
                    {
                        Name = actor.Name.Trim(),
                        Role = actor.Character,
                        Type = PersonKind.Actor,
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
                        Type = type == PersonType.Director ? PersonKind.Director : (type == PersonType.Producer ? PersonKind.Producer : PersonKind.Actor),
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

        private async Task<SearchCollection?> GetTmdbCollection(MovieInfo info, string tmdbId, CancellationToken cancellationToken)
        {

            var movieResult = await _tmdbApi
                            .GetMovieAsync(Convert.ToInt32(tmdbId, CultureInfo.InvariantCulture), info.MetadataLanguage, info.MetadataLanguage, cancellationToken)
                            .ConfigureAwait(false);
            if (movieResult != null)
            {
                return movieResult.BelongsToCollection;
            }

            return null;
        }

        private async Task<String?> GetTmdbOfficialRating(ItemLookupInfo info, string tmdbId, CancellationToken cancellationToken)
        {

            var movieResult = await _tmdbApi
                            .GetMovieAsync(Convert.ToInt32(tmdbId, CultureInfo.InvariantCulture), info.MetadataLanguage, info.MetadataLanguage, cancellationToken)
                            .ConfigureAwait(false);

            return GetTmdbOfficialRatingByData(movieResult, info.MetadataCountryCode);
        }

        private String? GetTmdbOfficialRatingByData(TMDbLib.Objects.Movies.Movie? movieResult, string preferredCountryCode)
        {
            if (movieResult == null || movieResult.Releases?.Countries == null)
            {
                return null;
            }

            var releases = movieResult.Releases.Countries.Where(i => !string.IsNullOrWhiteSpace(i.Certification)).ToList();

            var ourRelease = releases.FirstOrDefault(c => string.Equals(c.Iso_3166_1, preferredCountryCode, StringComparison.OrdinalIgnoreCase));
            var usRelease = releases.FirstOrDefault(c => string.Equals(c.Iso_3166_1, "US", StringComparison.OrdinalIgnoreCase));
            var minimumRelease = releases.FirstOrDefault();

            if (ourRelease != null)
            {
                var ratingPrefix = string.Equals(preferredCountryCode, "us", StringComparison.OrdinalIgnoreCase) ? string.Empty : preferredCountryCode + "-";
                var newRating = ratingPrefix + ourRelease.Certification;

                newRating = newRating.Replace("de-", "FSK-", StringComparison.OrdinalIgnoreCase);

                return newRating;
            }
            else if (usRelease != null)
            {
                return usRelease.Certification;
            }
            else if (minimumRelease != null)
            {
                return minimumRelease.Certification;
            }

            return null;
        }

    }
}
