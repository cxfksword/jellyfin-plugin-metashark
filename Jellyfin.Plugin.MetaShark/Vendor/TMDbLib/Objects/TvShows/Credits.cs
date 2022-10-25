using System.Collections.Generic;
using Newtonsoft.Json;
using TMDbLib.Objects.General;

namespace TMDbLib.Objects.TvShows
{
    public class Credits
    {
        [JsonProperty("cast")]
        public List<Cast> Cast { get; set; }

        [JsonProperty("crew")]
        public List<Crew> Crew { get; set; }

        [JsonProperty("id")]
        public int Id { get; set; }
    }
}