using Newtonsoft.Json;
using System.Collections.Generic;


namespace TMDbLib.Objects.Configuration
{
    public class APIConfigurationImages
    {
        [JsonProperty("base_url")]
        public string BaseUrl { get; set; }

        [JsonProperty("secure_base_url")]
        public string SecureBaseUrl { get; set; }

        [JsonProperty("backdrop_sizes")]
        public List<string> BackdropSizes { get; set; }

        [JsonProperty("logo_sizes")]
        public List<string> LogoSizes { get; set; }

        [JsonProperty("poster_sizes")]
        public List<string> PosterSizes { get; set; }

        [JsonProperty("profile_sizes")]
        public List<string> ProfileSizes { get; set; }

        [JsonProperty("still_sizes")]
        public List<string> StillSizes { get; set; }
    }
}
