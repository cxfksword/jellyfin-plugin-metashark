using Newtonsoft.Json;
using TMDbLib.Objects.People;

namespace TMDbLib.Objects.General
{
    public class Crew : CrewBase
    {
        [JsonProperty("credit_id")]
        public string CreditId { get; set; }

        [JsonProperty("job")]
        public string Job { get; set; }
    }
}