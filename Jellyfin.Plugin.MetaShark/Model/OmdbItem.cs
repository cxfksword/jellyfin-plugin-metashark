using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.MetaShark.Model
{
    public class OmdbItem
    {
        [JsonPropertyName("imdbID")]
        public string ImdbID { get; set; }
    }
}
