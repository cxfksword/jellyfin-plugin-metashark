using Newtonsoft.Json;

namespace TMDbLib.Objects.Search
{
    public class SearchTvShowWithRating : SearchTv
    {
        [JsonProperty("rating")]
        public double Rating { get; set; }
    }
}