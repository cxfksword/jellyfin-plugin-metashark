using Newtonsoft.Json;

namespace TMDbLib.Objects.Account
{
    public class Avatar
    {
        [JsonProperty("gravatar")]
        public Gravatar Gravatar { get; set; }
    }
}