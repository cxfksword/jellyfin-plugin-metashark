using Jellyfin.Plugin.MetaShark.Api;
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
    public class EpisodeImageProvider : BaseProvider, IRemoteImageProvider
    {
        public EpisodeImageProvider(IHttpClientFactory httpClientFactory, ILoggerFactory loggerFactory, ILibraryManager libraryManager, IHttpContextAccessor httpContextAccessor, DoubanApi doubanApi, TmdbApi tmdbApi, OmdbApi omdbApi, ImdbApi imdbApi)
            : base(httpClientFactory, loggerFactory.CreateLogger<EpisodeImageProvider>(), libraryManager, httpContextAccessor, doubanApi, tmdbApi, omdbApi, imdbApi)
        {
        }

        /// <inheritdoc />
        public string Name => Plugin.PluginName;

        /// <inheritdoc />
        public bool Supports(BaseItem item) => item is Episode;

        /// <inheritdoc />
        public IEnumerable<ImageType> GetSupportedImages(BaseItem item)
        {
            yield return ImageType.Primary;
        }

        /// <inheritdoc />
        public async Task<IEnumerable<RemoteImageInfo>> GetImages(BaseItem item, CancellationToken cancellationToken)
        {
            this.Log($"GetEpisodeImages of [name]: {item.Name} number: {item.IndexNumber} ParentIndexNumber: {item.ParentIndexNumber}");

            var episode = (MediaBrowser.Controller.Entities.TV.Episode)item;
            var series = episode.Series;

            var seriesTmdbId = Convert.ToInt32(series?.GetProviderId(MetadataProvider.Tmdb), CultureInfo.InvariantCulture);

            if (seriesTmdbId <= 0)
            {
                this.Log($"[GetEpisodeImages] The seriesTmdbId is empty!");
                return Enumerable.Empty<RemoteImageInfo>();
            }

            var seasonNumber = episode.ParentIndexNumber;
            var episodeNumber = episode.IndexNumber;

            if (seasonNumber is null or 0 || episodeNumber is null or 0)
            {
                this.Log($"[GetEpisodeImages] The seasonNumber or episodeNumber is empty! seasonNumber: {seasonNumber} episodeNumber: {episodeNumber}");
                return Enumerable.Empty<RemoteImageInfo>();
            }
            var language = item.GetPreferredMetadataLanguage();

            // 利用season缓存取剧集信息会更快
            var seasonResult = await this._tmdbApi
                .GetSeasonAsync(seriesTmdbId, seasonNumber.Value, null, null, cancellationToken)
                .ConfigureAwait(false);
            if (seasonResult == null || seasonResult.Episodes.Count < episodeNumber.Value)
            {
                this.Log($"[GetEpisodeImages] Can't get season data for seasonNumber: {seasonNumber} episodeNumber: {episodeNumber}");
                return Enumerable.Empty<RemoteImageInfo>();
            }

            var result = new List<RemoteImageInfo>();
            var episodeResult = seasonResult.Episodes[episodeNumber.Value - 1];
            if (!string.IsNullOrEmpty(episodeResult.StillPath))
            {
                result.Add(new RemoteImageInfo
                {
                    Url = this._tmdbApi.GetStillUrl(episodeResult.StillPath),
                    CommunityRating = episodeResult.VoteAverage,
                    VoteCount = episodeResult.VoteCount,
                    ProviderName = Name,
                    Type = ImageType.Primary
                });
            }
            return result;
        }

    }
}
