using Newtonsoft.Json;

namespace TMDbLib.Objects.Countries
{
    public class Country
    {
        [JsonProperty("iso_3166_1")]
        public string Iso_3166_1 { get; set; }

        [JsonProperty("english_name")]
        public string EnglishName { get; set; }
    }
}