using System;
using Newtonsoft.Json;

namespace TMDbLib.Objects.Credit
{
    public class CreditEpisode
    {
        [JsonProperty("air_date")]
        public DateTime? AirDate { get; set; }

        [JsonProperty("episode_number")]
        public int EpisodeNumber { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("overview")]
        public string Overview { get; set; }

        [JsonProperty("season_number")]
        public int SeasonNumber { get; set; }

        [JsonProperty("still_path")]
        public string StillPath { get; set; }
    }
}
