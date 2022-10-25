using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using TMDbLib.Objects.General;

namespace TMDbLib.Objects.Search
{
    public class SearchTv : SearchMovieTvBase
    {
        public SearchTv()
        {
            MediaType = MediaType.Tv;
        }

        [JsonProperty("first_air_date")]
        public DateTime? FirstAirDate { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("original_name")]
        public string OriginalName { get; set; }

        /// <summary>
        /// Country ISO code ex. US
        /// </summary>
        [JsonProperty("origin_country")]
        public List<string> OriginCountry { get; set; }
    }
}