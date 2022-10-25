using Newtonsoft.Json;
using System.Collections.Generic;

namespace TMDbLib.Objects.TvShows
{
    public class TvGroupCollection
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("type")]
        public TvGroupType Type { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("network")]
        public NetworkWithLogo Network { get; set; }

        [JsonProperty("episode_count")]
        public int EpisodeCount { get; set; }

        [JsonProperty("group_count")]
        public int GroupCount { get; set; }

        [JsonProperty("groups")]
        public List<TvGroup> Groups { get; set; }
    }
}