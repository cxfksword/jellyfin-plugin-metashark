using Newtonsoft.Json;

namespace TMDbLib.Objects.Account
{
    public class Gravatar
    {
        [JsonProperty("hash")]
        public string Hash { get; set; }
    }
}