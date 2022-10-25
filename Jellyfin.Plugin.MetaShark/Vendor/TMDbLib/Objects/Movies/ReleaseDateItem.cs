using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace TMDbLib.Objects.Movies
{
    public class ReleaseDateItem
    {
        [JsonProperty("certification")]
        public string Certification { get; set; }

        /// <summary>
        /// A language code, e.g. en
        /// </summary>
        [JsonProperty("iso_639_1")]
        public string Iso_639_1 { get; set; }

        [JsonProperty("note")]
        public string Note { get; set; }

        [JsonProperty("release_date")]
        [JsonConverter(typeof(IsoDateTimeConverter))]
        public DateTime ReleaseDate { get; set; }

        [JsonProperty("type")]
        public ReleaseDateType Type { get; set; }
    }
}