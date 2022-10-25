using Newtonsoft.Json;

namespace TMDbLib.Objects.General
{
    public class ExternalIdsPerson : ExternalIds
    {
        [JsonProperty("facebook_id")]
        public string FacebookId { get; set; }

        [JsonProperty("imdb_id")]
        public string ImdbId { get; set; }

        [JsonProperty("twitter_id")]
        public string TwitterId { get; set; }

        [JsonProperty("instagram_id")]
        public string InstagramId { get; set; }
    }
}