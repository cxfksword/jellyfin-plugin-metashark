using Newtonsoft.Json;

namespace TMDbLib.Objects.General
{
    public class ProductionCompany
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("logo_path")]
        public string LogoPath { get; set; }

        [JsonProperty("origin_country")]
        public string OriginCountry { get; set; }
    }
}
