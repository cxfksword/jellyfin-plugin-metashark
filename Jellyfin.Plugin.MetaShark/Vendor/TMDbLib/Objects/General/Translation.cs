using Newtonsoft.Json;

namespace TMDbLib.Objects.General
{
    public class Translation
    {
        [JsonProperty("english_name")]
        public string EnglishName { get; set; }

        /// <summary>
        /// A language code, e.g. en
        /// </summary>
        [JsonProperty("iso_639_1")]
        public string Iso_639_1 { get; set; }

        [JsonProperty("iso_3166_1")]
        public string Iso_3166_1 { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("data")]
        public TranslationData Data { get; set; }
    }
}