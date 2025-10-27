using System;
using System.Collections.Generic;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;

namespace Jellyfin.Plugin.MetaShark.Providers.ExternalId
{
    /// <summary>
    /// External URLs for Douban.
    /// </summary>
    public class DoubanExternalUrlProvider : IExternalUrlProvider
    {
        /// <inheritdoc/>
        public string Name => BaseProvider.DoubanProviderName;

        /// <inheritdoc/>
        public IEnumerable<string> GetExternalUrls(BaseItem item)
        {
            switch (item)
            {
                case Person:
                    if (item.TryGetProviderId(BaseProvider.DoubanProviderId, out var externalId))
                    {
                        yield return $"https://www.douban.com/personage/{externalId}/";
                    }

                    break;
                default:
                    if (item.TryGetProviderId(BaseProvider.DoubanProviderId, out externalId))
                    {
                        yield return $"https://movie.douban.com/subject/{externalId}/";
                    }

                    break;
            }
        }
    }
}