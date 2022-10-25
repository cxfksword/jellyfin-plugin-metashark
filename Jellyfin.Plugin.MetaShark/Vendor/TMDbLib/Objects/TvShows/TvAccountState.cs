using Newtonsoft.Json;

namespace TMDbLib.Objects.TvShows
{
    public class TvAccountState
    {
        [JsonProperty("rating")]
        public double? Rating { get; set; }
    }
}