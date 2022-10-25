using Newtonsoft.Json;

namespace TMDbLib.Objects.Search
{
    public class AccountSearchTv : SearchTv
    {
        [JsonProperty("rating")]
        public float Rating { get; set; }
    }
}