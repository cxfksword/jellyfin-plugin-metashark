using Newtonsoft.Json;

namespace TMDbLib.Objects.Search
{
    public class SearchCompany
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("logo_path")]
        public string LogoPath { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }
    }
}