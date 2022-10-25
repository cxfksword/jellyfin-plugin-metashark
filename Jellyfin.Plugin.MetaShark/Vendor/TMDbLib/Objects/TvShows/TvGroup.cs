using Newtonsoft.Json;
using System.Collections.Generic;

namespace TMDbLib.Objects.TvShows
{
    public class TvGroup
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("order")]
        public int Order { get; set; }

        [JsonProperty("episodes")]
        public List<TvGroupEpisode> Episodes { get; set; }

        [JsonProperty("locked")]
        public bool Locked { get; set; }
    }
}