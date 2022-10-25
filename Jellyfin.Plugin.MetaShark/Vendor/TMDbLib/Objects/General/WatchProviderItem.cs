using Newtonsoft.Json;

namespace TMDbLib.Objects.General
{
    public class WatchProviderItem
    {
        [JsonProperty("display_priority")]
        public int? DisplayPriority { get; set; }

        [JsonProperty("logo_path")]
        public string LogoPath { get; set; }

        [JsonProperty("provider_id")]
        public int? ProviderId { get; set; }

        [JsonProperty("provider_name")]
        public string ProviderName { get; set; }
    }
}