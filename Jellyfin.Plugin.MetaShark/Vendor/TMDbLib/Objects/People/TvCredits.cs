using System.Collections.Generic;
using Newtonsoft.Json;

namespace TMDbLib.Objects.People
{
    public class TvCredits
    {
        [JsonProperty("cast")]
        public List<TvRole> Cast { get; set; }

        [JsonProperty("crew")]
        public List<TvJob> Crew { get; set; }

        [JsonProperty("id")]
        public int Id { get; set; }
    }
}