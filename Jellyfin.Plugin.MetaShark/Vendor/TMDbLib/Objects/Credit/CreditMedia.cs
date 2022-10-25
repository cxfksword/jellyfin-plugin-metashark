using System.Collections.Generic;
using Newtonsoft.Json;

namespace TMDbLib.Objects.Credit
{
    public class CreditMedia
    {
        [JsonProperty("character")]
        public string Character { get; set; }

        [JsonProperty("episodes")]
        public List<CreditEpisode> Episodes { get; set; }

        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("original_name")]
        public string OriginalName { get; set; }

        [JsonProperty("seasons")]
        public List<CreditSeason> Seasons { get; set; }
    }
}