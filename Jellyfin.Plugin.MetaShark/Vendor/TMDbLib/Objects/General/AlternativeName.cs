using Newtonsoft.Json;

namespace TMDbLib.Objects.General
{
    public class AlternativeName
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }
    }
}