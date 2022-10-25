using Newtonsoft.Json;

namespace TMDbLib.Objects.TvShows
{
    public class TvGroupEpisode : TvEpisodeBase
    {
        [JsonProperty("order")]
        public int Order { get; set; }
    }
}