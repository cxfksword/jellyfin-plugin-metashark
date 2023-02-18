using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.MetaShark.Model
{
    public class GuessInfo
    {
        public int? episodeNumber { get; set; }

        public int? seasonNumber { get; set; }

        public string? Name { get; set; }
    }
}