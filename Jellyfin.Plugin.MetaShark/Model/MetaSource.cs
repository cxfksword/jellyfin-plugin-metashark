using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.MetaShark.Model
{

    public enum MetaSource
    {
        Douban,
        Tmdb,
        None
    }

    public static class MetaSourceExtensions
    {
        public static MetaSource ToMetaSource(this string? str)
        {
            if (str == null)
            {
                return MetaSource.None;
            }

            if (str.ToLower().StartsWith("douban"))
            {
                return MetaSource.Douban;
            }

            if (str.ToLower().StartsWith("tmdb"))
            {
                return MetaSource.Tmdb;
            }

            return MetaSource.None;
        }
    }
}
