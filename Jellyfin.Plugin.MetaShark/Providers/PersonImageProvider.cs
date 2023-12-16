using Jellyfin.Plugin.MetaShark.Api;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.MetaShark.Providers
{
    public class PersonImageProvider : BaseProvider, IRemoteImageProvider
    {
        public PersonImageProvider(IHttpClientFactory httpClientFactory, ILoggerFactory loggerFactory, ILibraryManager libraryManager, IHttpContextAccessor httpContextAccessor, DoubanApi doubanApi, TmdbApi tmdbApi, OmdbApi omdbApi, ImdbApi imdbApi)
            : base(httpClientFactory, loggerFactory.CreateLogger<PersonImageProvider>(), libraryManager, httpContextAccessor, doubanApi, tmdbApi, omdbApi, imdbApi)
        {
        }

        /// <inheritdoc />
        public string Name => Plugin.PluginName;

        /// <inheritdoc />
        public bool Supports(BaseItem item) => item is Person;

        /// <inheritdoc />
        public IEnumerable<ImageType> GetSupportedImages(BaseItem item)
        {
            yield return ImageType.Primary;
        }

        /// <inheritdoc />
        public async Task<IEnumerable<RemoteImageInfo>> GetImages(BaseItem item, CancellationToken cancellationToken)
        {
            var list = new List<RemoteImageInfo>();
            var cid = item.GetProviderId(DoubanProviderId);
            var metaSource = item.GetProviderId(Plugin.ProviderId);
            this.Log($"GetImages for item: {item.Name} [metaSource]: {metaSource}");
            if (!string.IsNullOrEmpty(cid))
            {
                var celebrity = await this._doubanApi.GetCelebrityAsync(cid, cancellationToken).ConfigureAwait(false);
                if (celebrity != null)
                {
                    list.Add(new RemoteImageInfo
                    {
                        ProviderName = this.Name,
                        Url = this.GetProxyImageUrl(celebrity.Img),
                        Type = ImageType.Primary,
                    });
                }

                var photos = await this._doubanApi.GetCelebrityPhotosAsync(cid, cancellationToken).ConfigureAwait(false);
                photos.ForEach(x =>
                {
                    // 过滤不是竖图
                    if (x.Width < 400 || x.Height < x.Width * 1.3)
                    {
                        return;
                    }

                    list.Add(new RemoteImageInfo
                    {
                        ProviderName = this.Name,
                        Url = this.GetProxyImageUrl(x.Raw),
                        Width = x.Width,
                        Height = x.Height,
                        Type = ImageType.Primary,
                    });
                });
            }

            if (list.Count == 0)
            {
                this.Log($"Got images failed because the images of \"{item.Name}\" is empty!");
            }
            return list;
        }

    }
}
