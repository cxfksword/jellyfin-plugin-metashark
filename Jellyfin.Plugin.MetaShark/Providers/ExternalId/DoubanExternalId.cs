using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.MetaShark.Providers.ExternalId
{
    public class DoubanExternalId : IExternalId
    {
        public string ProviderName => BaseProvider.DoubanProviderName;

        public string Key => BaseProvider.DoubanProviderId;

        public ExternalIdMediaType? Type => null;

        public bool Supports(IHasProviderIds item)
        {
            return item is Movie || item is Series || item is Season;
        }
    }
}
