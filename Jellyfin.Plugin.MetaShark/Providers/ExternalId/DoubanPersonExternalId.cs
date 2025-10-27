using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;

namespace Jellyfin.Plugin.MetaShark.Providers.ExternalId
{
    /// <inheritdoc />
    public class DoubanPersonExternalId : IExternalId
    {
        /// <inheritdoc />
        public string ProviderName => BaseProvider.DoubanProviderName;

        /// <inheritdoc />
        public string Key => BaseProvider.DoubanProviderId;

        /// <inheritdoc />
        public ExternalIdMediaType? Type => ExternalIdMediaType.Person;

        /// <inheritdoc />
        public bool Supports(IHasProviderIds item) => item is Person;
    }
}