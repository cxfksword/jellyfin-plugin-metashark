using Newtonsoft.Json;

namespace TMDbLib.Objects.General
{
    public class WatchProviderRegion
    {
        /// <summary>
        /// A country code, e.g. US
        /// </summary>
        [JsonProperty("iso_3166_1")]
        public string Iso_3166_1 { get; set; }

        [JsonProperty("english_name")]
        public string EnglishName { get; set; }

        [JsonProperty("native_name")]
        public string NativeName { get; set; }
    }
}