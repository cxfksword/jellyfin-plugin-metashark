using System;
using Newtonsoft.Json;

namespace TMDbLib.Objects.General
{
    public class DateRange
    {
        [JsonProperty("maximum")]
        public DateTime Maximum { get; set; }

        [JsonProperty("minimum")]
        public DateTime Minimum { get; set; }
    }
}