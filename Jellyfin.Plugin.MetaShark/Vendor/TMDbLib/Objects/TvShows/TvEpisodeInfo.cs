using Newtonsoft.Json;

namespace TMDbLib.Objects.TvShows
{
    public class TvEpisodeInfo
    {
        [JsonProperty("id")]
        public int? Id { get; set; }

        [JsonProperty("season_number")]
        public int SeasonNumber { get; set; }

        [JsonProperty("episode_number")]
        public int EpisodeNumber { get; set; }
    }
}
