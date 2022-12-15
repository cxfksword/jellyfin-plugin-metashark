using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using MediaBrowser.Controller.Providers;

namespace Jellyfin.Plugin.MetaShark.Model
{
    public class ParseNameResult : ItemLookupInfo
    {
        public string ChineseName { get; set; }

        public bool IsSpecial { get; set; }
    }
}
