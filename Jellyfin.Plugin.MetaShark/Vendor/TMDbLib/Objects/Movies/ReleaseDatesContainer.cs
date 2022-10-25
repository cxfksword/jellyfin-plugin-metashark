using System.Collections.Generic;
using Newtonsoft.Json;

namespace TMDbLib.Objects.Movies
{
    public class ReleaseDatesContainer
    {
        /// <summary>
        /// A country code, e.g. US
        /// </summary>
        [JsonProperty("iso_3166_1")]
        public string Iso_3166_1 { get; set; }

        [JsonProperty("release_dates")]
        public List<ReleaseDateItem> ReleaseDates { get; set; }
    }
}