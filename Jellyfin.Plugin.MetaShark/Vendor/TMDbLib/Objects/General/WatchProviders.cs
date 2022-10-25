using System.Collections.Generic;
using Newtonsoft.Json;

namespace TMDbLib.Objects.General
{
    public class WatchProviders
    {
        [JsonProperty("link")]
        public string Link { get; set; }

        [JsonProperty("flatrate")]
        public List<WatchProviderItem> FlatRate { get; set; }

        [JsonProperty("rent")]
        public List<WatchProviderItem> Rent { get; set; }

        [JsonProperty("buy")]
        public List<WatchProviderItem> Buy { get; set; }
         
        [JsonProperty("free")]
        public List<WatchProviderItem> Free { get; set; }

        [JsonProperty("ads")]
        public List<WatchProviderItem> Ads { get; set; }
    }
}
