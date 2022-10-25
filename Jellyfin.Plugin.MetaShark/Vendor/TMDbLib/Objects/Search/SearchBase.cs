using Newtonsoft.Json;
using TMDbLib.Objects.General;

namespace TMDbLib.Objects.Search
{
    public class SearchBase
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("media_type")]
        public MediaType MediaType { get; set; }

        [JsonProperty("popularity")]
        public double Popularity { get; set; }
    }
}