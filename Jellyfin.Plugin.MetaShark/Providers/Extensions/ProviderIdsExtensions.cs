
using System.Collections.Generic;
using Jellyfin.Plugin.MetaShark.Model;
using MediaBrowser.Model.Entities;

namespace Jellyfin.Plugin.MetaShark.Providers
{
    public static class ProviderIdsExtensions
    {
        public static MetaSource GetMetaSource(this IHasProviderIds instance, string name)
        {
            var value = instance.GetProviderId(name);
            return value.ToMetaSource();
        }

        public static void TryGetMetaSource(this Dictionary<string, string> dict, string name, out MetaSource metaSource)
        {
            if (dict.TryGetValue(name, out var value))
            {
                metaSource = value.ToMetaSource();
            }
            else
            {
                metaSource = MetaSource.None;
            }
        }
    }
}