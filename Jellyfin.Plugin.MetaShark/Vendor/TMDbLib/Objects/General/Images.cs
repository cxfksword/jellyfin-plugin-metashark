using System.Collections.Generic;
using Newtonsoft.Json;

namespace TMDbLib.Objects.General
{
    public class Images
    {
        [JsonProperty("backdrops")]
        public List<ImageData> Backdrops { get; set; }

        [JsonProperty("posters")]
        public List<ImageData> Posters { get; set; }

        [JsonProperty("logos")]
        public List<ImageData> Logos { get; set; }
    }
}