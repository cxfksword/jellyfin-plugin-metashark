using Jellyfin.Plugin.MetaShark.Api;
using Jellyfin.Plugin.MetaShark.Core;
using Jellyfin.Plugin.MetaShark.Model;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Extensions;
using MediaBrowser.Model.Providers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.MetaShark.Providers
{
    public class SeriesImageProvider : BaseProvider, IRemoteImageProvider
    {
        public SeriesImageProvider(IHttpClientFactory httpClientFactory, ILoggerFactory loggerFactory, ILibraryManager libraryManager, IHttpContextAccessor httpContextAccessor, DoubanApi doubanApi, TmdbApi tmdbApi, OmdbApi omdbApi, ImdbApi imdbApi)
            : base(httpClientFactory, loggerFactory.CreateLogger<SeriesImageProvider>(), libraryManager, httpContextAccessor, doubanApi, tmdbApi, omdbApi, imdbApi)
        {
        }

        /// <inheritdoc />
        public string Name => Plugin.PluginName;

        /// <inheritdoc />
        public bool Supports(BaseItem item) => item is Series;

        /// <inheritdoc />
        public IEnumerable<ImageType> GetSupportedImages(BaseItem item) => new List<ImageType>
        {
            ImageType.Primary,
            ImageType.Backdrop,
            ImageType.Logo,
        };

        /// <inheritdoc />
        public async Task<IEnumerable<RemoteImageInfo>> GetImages(BaseItem item, CancellationToken cancellationToken)
        {
            var sid = item.GetProviderId(DoubanProviderId);
            var metaSource = item.GetMetaSource(Plugin.ProviderId);
            this.Log($"GetImages for item: {item.Name} [metaSource]: {metaSource}");
            if (metaSource != MetaSource.Tmdb && !string.IsNullOrEmpty(sid))
            {
                var primary = await this._doubanApi.GetMovieAsync(sid, cancellationToken).ConfigureAwait(false);
                if (primary == null || string.IsNullOrEmpty(primary.Img))
                {
                    return Enumerable.Empty<RemoteImageInfo>();
                }
                var backdropImgs = await this.GetBackdrop(item, cancellationToken).ConfigureAwait(false);
                var logoImgs = await this.GetLogos(item, cancellationToken).ConfigureAwait(false);

                var res = new List<RemoteImageInfo> {
                    new RemoteImageInfo
                    {
                        ProviderName = this.Name,
                        Url = this.GetDoubanPoster(primary),
                        Type = ImageType.Primary,
                    },
                };
                res.AddRange(backdropImgs);
                res.AddRange(logoImgs);
                return res;
            }

            var tmdbId = item.GetProviderId(MetadataProvider.Tmdb);
            if (metaSource == MetaSource.Tmdb && !string.IsNullOrEmpty(tmdbId))
            {
                var language = item.GetPreferredMetadataLanguage();
                // 设定language会导致图片被过滤，这里设为null，保持取全部语言图片
                var movie = await this._tmdbApi
                .GetSeriesAsync(tmdbId.ToInt(), null, null, cancellationToken)
                .ConfigureAwait(false);

                if (movie?.Images == null)
                {
                    return Enumerable.Empty<RemoteImageInfo>();
                }

                var remoteImages = new List<RemoteImageInfo>();

                remoteImages.AddRange(movie.Images.Posters.Select(x => new RemoteImageInfo {
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

                remoteImages.AddRange(movie.Images.Backdrops.Select(x => new RemoteImageInfo {
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

                remoteImages.AddRange(movie.Images.Logos.Select(x => new RemoteImageInfo {
                        ProviderName = this.Name,
                        Url = this._tmdbApi.GetLogoUrl(x.FilePath),
                        Type = ImageType.Logo,
                        CommunityRating = x.VoteAverage,
                        VoteCount = x.VoteCount,
                        Width = x.Width,
                        Height = x.Height,
                        Language = this.AdjustImageLanguage(x.Iso_639_1, language),
                        RatingType = RatingType.Score,
                    }));

                return remoteImages.OrderByLanguageDescending(language);
            }

            this.Log($"Got images failed because the images of \"{item.Name}\" is empty!");
            return new List<RemoteImageInfo>();
        }

        /// <summary>
        /// Query for a background photo
        /// </summary>
        /// <param name="cancellationToken">Instance of the <see cref="CancellationToken"/> interface.</param>
        private async Task<IEnumerable<RemoteImageInfo>> GetBackdrop(BaseItem item, CancellationToken cancellationToken)
        {
            var sid = item.GetProviderId(DoubanProviderId);
            var tmdbId = item.GetProviderId(MetadataProvider.Tmdb);
            var list = new List<RemoteImageInfo>();

            // 从豆瓣获取背景图
            if (!string.IsNullOrEmpty(sid))
            {
                var photo = await this._doubanApi.GetWallpaperBySidAsync(sid, cancellationToken);
                if (photo != null && photo.Count > 0)
                {
                    this.Log("GetBackdrop from douban sid: {0}", sid);
                    list = photo.Where(x => x.Width >= 1280 && x.Width <= 4096 && x.Width > x.Height * 1.3).Select(x =>
                    {
                        if (config.EnableDoubanBackdropRaw)
                        {
                            return new RemoteImageInfo
                            {
                                ProviderName = Name,
                                Url = this.GetProxyImageUrl(x.Raw),
                                Height = x.Height,
                                Width = x.Width,
                                Type = ImageType.Backdrop,
                            };
                        }
                        else
                        {
                            return new RemoteImageInfo
                            {
                                ProviderName = Name,
                                Url = this.GetProxyImageUrl(x.Large),
                                Type = ImageType.Backdrop,
                            };
                        }
                    }).ToList();

                }
            }

            // 背景图缺失，从TheMovieDb补充背景图
            if (list.Count == 0 && config.EnableTmdbBackdrop && !string.IsNullOrEmpty(tmdbId))
            {
                var language = item.GetPreferredMetadataLanguage();
                var movie = await _tmdbApi
                .GetSeriesAsync(tmdbId.ToInt(), language, language, cancellationToken)
                .ConfigureAwait(false);

                if (movie != null && !string.IsNullOrEmpty(movie.BackdropPath))
                {
                    this.Log("GetBackdrop from tmdb id: {0}", tmdbId);
                    list.Add(new RemoteImageInfo
                    {
                        ProviderName = this.Name,
                        Url = this._tmdbApi.GetBackdropUrl(movie.BackdropPath),
                        Type = ImageType.Backdrop,
                    });
                }
            }

            return list;
        }

        private async Task<IEnumerable<RemoteImageInfo>> GetLogos(BaseItem item, CancellationToken cancellationToken)
        {
            var tmdbId = item.GetProviderId(MetadataProvider.Tmdb);
            var language = item.GetPreferredMetadataLanguage();
            var list = new List<RemoteImageInfo>();
            if (this.config.EnableTmdbLogo && !string.IsNullOrEmpty(tmdbId))
            {
                this.Log("GetLogos from tmdb id: {0} lang: {1}", tmdbId, language);
                var movie = await this._tmdbApi
                .GetSeriesAsync(tmdbId.ToInt(), language, language, cancellationToken)
                .ConfigureAwait(false);

                if (movie != null && movie.Images != null)
                {
                    list.AddRange(movie.Images.Logos.Select(x => new RemoteImageInfo {
                        ProviderName = this.Name,
                        Url = this._tmdbApi.GetLogoUrl(x.FilePath),
                        Type = ImageType.Logo,
                        CommunityRating = x.VoteAverage,
                        VoteCount = x.VoteCount,
                        Width = x.Width,
                        Height = x.Height,
                        Language = this.AdjustImageLanguage(x.Iso_639_1, language),
                        RatingType = RatingType.Score,
                    }));
                }
            }

            return list.OrderByLanguageDescending(language);
        }

    }
}
