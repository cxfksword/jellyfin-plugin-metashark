using Newtonsoft.Json;

namespace TMDbLib.Objects.TvShows
{
    public class TvEpisodeAccountStateWithNumber : TvEpisodeAccountState
    {
        [JsonProperty("episode_number")]
        public int EpisodeNumber { get; set; }
    }
}