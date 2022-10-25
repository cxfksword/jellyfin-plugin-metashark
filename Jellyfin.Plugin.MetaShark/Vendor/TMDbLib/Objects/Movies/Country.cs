using System;
using Newtonsoft.Json;

namespace TMDbLib.Objects.Movies
{
    public class Country
    {
        [JsonProperty("certification")]
        public string Certification { get; set; }

        /// <summary>
        /// A country code, e.g. US
        /// </summary>
        [JsonProperty("iso_3166_1")]
        public string Iso_3166_1 { get; set; }

        [JsonProperty("primary")]
        public bool Primary { get; set; }

        [JsonProperty("release_date")]
        public DateTime? ReleaseDate { get; set; }
    }
}
