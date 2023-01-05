using Jellyfin.Plugin.MetaShark.Api;
using Jellyfin.Plugin.MetaShark.Core;
using Jellyfin.Plugin.MetaShark.Model;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Extensions;
using MediaBrowser.Model.Providers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TMDbLib.Objects.Languages;

namespace Jellyfin.Plugin.MetaShark.Providers
{
    public class MovieImageProvider : BaseProvider, IRemoteImageProvider
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MovieImageProvider"/> class.
        /// </summary>
        /// <param name="httpClientFactory">Instance of the <see cref="IHttpClientFactory"/> interface.</param>
        /// <param name="logger">Instance of the <see cref="ILogger{OddbImageProvider}"/> interface.</param>
        /// <param name="doubanApi">Instance of <see cref="DoubanApi"/>.</param>
        public MovieImageProvider(IHttpClientFactory httpClientFactory, ILoggerFactory loggerFactory, ILibraryManager libraryManager, IHttpContextAccessor httpContextAccessor, DoubanApi doubanApi, TmdbApi tmdbApi, OmdbApi omdbApi)
            : base(httpClientFactory, loggerFactory.CreateLogger<MovieProvider>(), libraryManager, httpContextAccessor, doubanApi, tmdbApi, omdbApi)
        {
        }

        /// <inheritdoc />
        public string Name => Plugin.PluginName;

        /// <inheritdoc />
        public bool Supports(BaseItem item) => item is Movie;

        /// <inheritdoc />
        public IEnumerable<ImageType> GetSupportedImages(BaseItem item) => new List<ImageType>
        {
            ImageType.Primary,
            ImageType.Backdrop
        };

        /// <inheritdoc />
        public async Task<IEnumerable<RemoteImageInfo>> GetImages(BaseItem item, CancellationToken cancellationToken)
        {
            var sid = item.GetProviderId(DoubanProviderId);
            var metaSource = item.GetProviderId(Plugin.ProviderId);
            this.Log($"GetImages for item: {item.Name} [metaSource]: {metaSource}");
            if (metaSource != MetaSource.Tmdb && !string.IsNullOrEmpty(sid))
            {
                var primary = await this._doubanApi.GetMovieAsync(sid, cancellationToken);
                if (primary == null)
                {
                    return Enumerable.Empty<RemoteImageInfo>();
                }
                var backdropImgs = await GetBackdrop(sid, cancellationToken);

                var res = new List<RemoteImageInfo> {
                    new RemoteImageInfo
                    {
                        ProviderName = primary.Name,
                        Url = primary.ImgMiddle,
                        Type = ImageType.Primary
                    }
                };
                res.AddRange(backdropImgs);
                return res;
            }

            var tmdbId = item.GetProviderId(MetadataProvider.Tmdb);
            if (!string.IsNullOrEmpty(tmdbId))
            {
                var language = item.GetPreferredMetadataLanguage();
                var movie = await _tmdbApi
                .GetMovieAsync(tmdbId.ToInt(), language, language, cancellationToken)
                .ConfigureAwait(false);

                if (movie?.Images == null)
                {
                    return Enumerable.Empty<RemoteImageInfo>();
                }

                var remoteImages = new List<RemoteImageInfo>();

                for (var i = 0; i < movie.Images.Posters.Count; i++)
                {
                    var poster = movie.Images.Posters[i];
                    remoteImages.Add(new RemoteImageInfo
                    {
                        Url = _tmdbApi.GetPosterUrl(poster.FilePath),
                        CommunityRating = poster.VoteAverage,
                        VoteCount = poster.VoteCount,
                        Width = poster.Width,
                        Height = poster.Height,
                        ProviderName = Name,
                        Type = ImageType.Primary,
                    });
                }

                for (var i = 0; i < movie.Images.Backdrops.Count; i++)
                {
                    var backdrop = movie.Images.Backdrops[i];
                    remoteImages.Add(new RemoteImageInfo
                    {
                        Url = _tmdbApi.GetPosterUrl(backdrop.FilePath),
                        CommunityRating = backdrop.VoteAverage,
                        VoteCount = backdrop.VoteCount,
                        Width = backdrop.Width,
                        Height = backdrop.Height,
                        ProviderName = Name,
                        Type = ImageType.Backdrop,
                        RatingType = RatingType.Score
                    });
                }

                return remoteImages.OrderByLanguageDescending(language);
            }

            this.Log($"Got images failed because the sid of \"{item.Name}\" is empty!");
            return new List<RemoteImageInfo>();
        }

        /// <inheritdoc />
        public async Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            this.Log("GetImageResponse url: {0}", url);
            return await this._httpClientFactory.CreateClient().GetAsync(new Uri(url), cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Query for a background photo
        /// </summary>
        /// <param name="sid">a subject/movie id</param>
        /// <param name="cancellationToken">Instance of the <see cref="CancellationToken"/> interface.</param>
        private async Task<IEnumerable<RemoteImageInfo>> GetBackdrop(string sid, CancellationToken cancellationToken)
        {
            this.Log("GetBackdrop of sid: {0}", sid);
            var photo = await this._doubanApi.GetWallpaperBySidAsync(sid, cancellationToken);
            var list = new List<RemoteImageInfo>();

            if (photo == null)
            {
                return list;
            }

            return photo.Where(x => x.Width > x.Height * 1.3).Select(x =>
            {
                return new RemoteImageInfo
                {
                    ProviderName = Name,
                    Url = x.Large,
                    Type = ImageType.Backdrop,
                };
            });
        }

    }
}
