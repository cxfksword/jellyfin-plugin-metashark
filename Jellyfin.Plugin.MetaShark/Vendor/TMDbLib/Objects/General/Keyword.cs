using Newtonsoft.Json;

namespace TMDbLib.Objects.General
{
    public class Keyword
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }
    }
}