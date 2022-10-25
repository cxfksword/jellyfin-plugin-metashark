using Newtonsoft.Json;

namespace TMDbLib.Objects.Credit
{
    public class CreditPerson
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }
    }
}