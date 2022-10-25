using Newtonsoft.Json;

namespace TMDbLib.Objects.General
{
    public class SearchContainerWithDates<T> : SearchContainer<T>
    {
        [JsonProperty("dates")]
        public DateRange Dates { get; set; }
    }
}