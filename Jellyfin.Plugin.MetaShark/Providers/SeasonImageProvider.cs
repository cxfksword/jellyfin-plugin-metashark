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
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TMDbLib.Objects.Languages;

namespace Jellyfin.Plugin.MetaShark.Providers
{
    public class SeasonImageProvider : BaseProvider, IRemoteImageProvider
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SeasonImageProvider"/> class.
        /// </summary>
        /// <param name="httpClientFactory">Instance of the <see cref="IHttpClientFactory"/> interface.</param>
        /// <param name="logger">Instance of the <see cref="ILogger{OddbImageProvider}"/> interface.</param>
        /// <param name="doubanApi">Instance of <see cref="DoubanApi"/>.</param>
        public SeasonImageProvider(IHttpClientFactory httpClientFactory, ILoggerFactory loggerFactory, ILibraryManager libraryManager, DoubanApi doubanApi, TmdbApi tmdbApi, OmdbApi omdbApi)
            : base(httpClientFactory, loggerFactory.CreateLogger<SeriesProvider>(), libraryManager, doubanApi, tmdbApi, omdbApi)
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
            var metaSource = series.GetProviderId(Plugin.ProviderId);

            // get image from douban
            var sid = item.GetProviderId(DoubanProviderId);
            if (metaSource == MetaSource.Douban && !string.IsNullOrEmpty(sid))
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
                        Url = primary.ImgMiddle,
                        Type = ImageType.Primary
                    }
                };
                return res;
            }


            // get image form TMDB
            var seriesTmdbId = Convert.ToInt32(series?.GetProviderId(MetadataProvider.Tmdb), CultureInfo.InvariantCulture);
            Console.WriteLine($"seriesTmdbId={seriesTmdbId}  season?.IndexNumber={season?.IndexNumber}");
            if (seriesTmdbId <= 0 || season?.IndexNumber == null)
            {
                return Enumerable.Empty<RemoteImageInfo>();
            }

            var language = item.GetPreferredMetadataLanguage();

            var seasonResult = await this._tmdbApi
                .GetSeasonAsync(seriesTmdbId, season.IndexNumber.Value, language, language, cancellationToken)
                .ConfigureAwait(false);
            var posters = seasonResult?.Images?.Posters;
            Console.WriteLine(posters?.ToJson());
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
                    ProviderName = Name,
                    Type = ImageType.Primary,
                };
            }

            return remoteImages.OrderByLanguageDescending(language);
        }

        /// <inheritdoc />
        public async Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            this.Log("GetImageResponse url: {0}", url);
            return await this._httpClientFactory.CreateClient().GetAsync(new Uri(url), cancellationToken).ConfigureAwait(false);
        }

    }
}
