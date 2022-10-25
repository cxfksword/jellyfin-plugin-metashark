using System;
using Newtonsoft.Json;

namespace TMDbLib.Objects.Credit
{
    public class CreditSeason
    {
        [JsonProperty("air_date")]
        public DateTime? AirDate { get; set; }

        [JsonProperty("poster_path")]
        public string PosterPath { get; set; }

        [JsonProperty("season_number")]
        public int SeasonNumber { get; set; }
    }
}