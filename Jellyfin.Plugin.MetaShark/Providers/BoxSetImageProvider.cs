using Jellyfin.Plugin.MetaShark.Api;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
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
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.MetaShark.Providers
{
    /// <summary>
    /// BoxSet image provider powered by TMDb.
    /// </summary>
    public class BoxSetImageProvider : BaseProvider, IRemoteImageProvider
    {
        public BoxSetImageProvider(IHttpClientFactory httpClientFactory, ILoggerFactory loggerFactory, ILibraryManager libraryManager, IHttpContextAccessor httpContextAccessor, DoubanApi doubanApi, TmdbApi tmdbApi, OmdbApi omdbApi, ImdbApi imdbApi)
            : base(httpClientFactory, loggerFactory.CreateLogger<BoxSetImageProvider>(), libraryManager, httpContextAccessor, doubanApi, tmdbApi, omdbApi, imdbApi)
        {
        }

        /// <inheritdoc />
        public string Name => Plugin.PluginName;

        /// <inheritdoc />
        public bool Supports(BaseItem item)
        {
            return item is BoxSet;
        }

        /// <inheritdoc />
        public IEnumerable<ImageType> GetSupportedImages(BaseItem item) =>
        [
            ImageType.Primary,
            ImageType.Backdrop,
            ImageType.Thumb
        ];

        /// <inheritdoc />
        public async Task<IEnumerable<RemoteImageInfo>> GetImages(BaseItem item, CancellationToken cancellationToken)
        {
            var tmdbId = Convert.ToInt32(item.GetProviderId(MetadataProvider.Tmdb), CultureInfo.InvariantCulture);
            this.Log($"GetBoxSetImages of [name]: {item.Name} [tmdbId]: {tmdbId}");

            if (tmdbId <= 0)
            {
                return Enumerable.Empty<RemoteImageInfo>();
            }

            var language = item.GetPreferredMetadataLanguage();

            // TODO use image languages if All Languages isn't toggled, but there's currently no way to get that value in here
            var collection = await this._tmdbApi.GetCollectionAsync(tmdbId, null, null, cancellationToken).ConfigureAwait(false);

            if (collection?.Images is null)
            {
                return Enumerable.Empty<RemoteImageInfo>();
            }

            var posters = collection.Images.Posters;
            var backdrops = collection.Images.Backdrops;
            var remoteImages = new List<RemoteImageInfo>(posters.Count + backdrops.Count);
            remoteImages.AddRange(posters.Select(x => new RemoteImageInfo {
                    ProviderName = this.Name,
                    Url = this._tmdbApi.GetPosterUrl(x.FilePath),
                    Type = ImageType.Primary,
                    CommunityRating = x.VoteAverage,
                    VoteCount = x.VoteCount,
                    Width = x.Width,
                    Height = x.Height,
                    Language = this.AdjustImageLanguage(x.Iso_639_1, language),
                    RatingType = RatingType.Score,
                }));

            remoteImages.AddRange(backdrops.Select(x => new RemoteImageInfo {
                    ProviderName = this.Name,
                    Url = this._tmdbApi.GetBackdropUrl(x.FilePath),
                    Type = ImageType.Backdrop,
                    CommunityRating = x.VoteAverage,
                    VoteCount = x.VoteCount,
                    Width = x.Width,
                    Height = x.Height,
                    Language = this.AdjustImageLanguage(x.Iso_639_1, language),
                    RatingType = RatingType.Score,
                }));

            return remoteImages.OrderByLanguageDescending(language);
        }
    }
}