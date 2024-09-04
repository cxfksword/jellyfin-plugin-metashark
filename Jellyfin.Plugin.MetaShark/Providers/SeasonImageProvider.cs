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

namespace Jellyfin.Plugin.MetaShark.Providers
{
    public class SeasonImageProvider : BaseProvider, IRemoteImageProvider
    {
        public SeasonImageProvider(IHttpClientFactory httpClientFactory, ILoggerFactory loggerFactory, ILibraryManager libraryManager, IHttpContextAccessor httpContextAccessor, DoubanApi doubanApi, TmdbApi tmdbApi, OmdbApi omdbApi, ImdbApi imdbApi)
            : base(httpClientFactory, loggerFactory.CreateLogger<SeasonImageProvider>(), libraryManager, httpContextAccessor, doubanApi, tmdbApi, omdbApi, imdbApi)
        {
        }

        /// <inheritdoc />
        public string Name => Plugin.PluginName;

        /// <inheritdoc />
        public bool Supports(BaseItem item) => item is Season;

        /// <inheritdoc />
        public IEnumerable<ImageType> GetSupportedImages(BaseItem item)
        {
            yield return ImageType.Primary;
        }

        /// <inheritdoc />
        public async Task<IEnumerable<RemoteImageInfo>> GetImages(BaseItem item, CancellationToken cancellationToken)
        {
            this.Log($"GetSeasonImages for item: {item.Name} number: {item.IndexNumber}");
            var season = (Season)item;
            var series = season.Series;
            var metaSource = series.GetMetaSource(Plugin.ProviderId);

            // get image from douban
            var sid = item.GetProviderId(DoubanProviderId);
            if (metaSource != MetaSource.Tmdb && !string.IsNullOrEmpty(sid))
            {
                var primary = await this._doubanApi.GetMovieAsync(sid, cancellationToken).ConfigureAwait(false);
                if (primary == null)
                {
                    return Enumerable.Empty<RemoteImageInfo>();
                }

                var res = new List<RemoteImageInfo> {
                    new RemoteImageInfo
                    {
                        ProviderName = primary.Name,
                        Url = this.GetDoubanPoster(primary),
                        Type = ImageType.Primary,
                        Language = "zh",
                    },
                };
                return res;
            }


            // get image form TMDB
            var seriesTmdbId = Convert.ToInt32(series?.GetProviderId(MetadataProvider.Tmdb), CultureInfo.InvariantCulture);
            if (seriesTmdbId <= 0 || season?.IndexNumber == null)
            {
                return Enumerable.Empty<RemoteImageInfo>();
            }

            var language = item.GetPreferredMetadataLanguage();
            var seasonResult = await this._tmdbApi
                .GetSeasonAsync(seriesTmdbId, season.IndexNumber.Value, null, null, cancellationToken)
                .ConfigureAwait(false);
            var posters = seasonResult?.Images?.Posters;
            if (posters == null)
            {
                return Enumerable.Empty<RemoteImageInfo>();
            }

            var remoteImages = new RemoteImageInfo[posters.Count];
            for (var i = 0; i < posters.Count; i++)
            {
                var image = posters[i];
                remoteImages[i] = new RemoteImageInfo
                {
                    Url = this._tmdbApi.GetPosterUrl(image.FilePath),
                    CommunityRating = image.VoteAverage,
                    VoteCount = image.VoteCount,
                    Width = image.Width,
                    Height = image.Height,
                    Language = AdjustImageLanguage(image.Iso_639_1, language),
                    ProviderName = Name,
                    Type = ImageType.Primary,
                };
            }

            return remoteImages.OrderByLanguageDescending(language);
        }

    }
}
