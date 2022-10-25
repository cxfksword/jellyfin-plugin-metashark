using Newtonsoft.Json;

namespace TMDbLib.Objects.General
{
    public class CrewJob
    {
        [JsonProperty("job")]
        public string Job { get; set; }

        [JsonProperty("credit_id")]
        public string CreditId { get; set; }

        [JsonProperty("episode_count")]
        public int EpisodeCount { get; set; }
    }
}