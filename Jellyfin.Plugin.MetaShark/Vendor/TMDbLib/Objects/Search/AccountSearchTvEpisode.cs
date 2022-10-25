using Newtonsoft.Json;

namespace TMDbLib.Objects.Search
{
    public class AccountSearchTvEpisode : SearchTvEpisode
    {
        [JsonProperty("rating")]
        public double Rating { get; set; }
    }
}