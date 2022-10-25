using System.Collections.Generic;
using Newtonsoft.Json;
using TMDbLib.Objects.General;

namespace TMDbLib.Objects.TvShows
{
    public class CreditsAggregate
    {
        [JsonProperty("cast")]
        public List<CastAggregate> Cast { get; set; }

        [JsonProperty("crew")]
        public List<CrewAggregate> Crew { get; set; }

        [JsonProperty("id")]
        public int Id { get; set; }
    }
}