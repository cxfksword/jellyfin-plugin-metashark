using System.Collections.Generic;
using Newtonsoft.Json;

namespace TMDbLib.Objects.General
{
    public class ConfigImageTypes
    {
        [JsonProperty("backdrop_sizes")]
        public List<string> BackdropSizes { get; set; }

        [JsonProperty("base_url")]
        public string BaseUrl { get; set; }

        [JsonProperty("logo_sizes")]
        public List<string> LogoSizes { get; set; }

        [JsonProperty("poster_sizes")]
        public List<string> PosterSizes { get; set; }

        [JsonProperty("profile_sizes")]
        public List<string> ProfileSizes { get; set; }

        [JsonProperty("secure_base_url")]
        public string SecureBaseUrl { get; set; }

        [JsonProperty("still_sizes")]
        public List<string> StillSizes { get; set; }
    }
}