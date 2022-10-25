using Newtonsoft.Json;

namespace TMDbLib.Objects.Languages
{
    public class Language
    {
        [JsonProperty("iso_639_1")]
        public string Iso_639_1 { get; set; }

        [JsonProperty("english_name")]
        public string EnglishName { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }
    }
}