using Newtonsoft.Json;
using TMDbLib.Utilities.Converters;

namespace TMDbLib.Objects.Changes
{
    public class ChangesListItem
    {
        [JsonProperty("adult")]
        public bool? Adult { get; set; }

        [JsonProperty("id")]
        [JsonConverter(typeof(TmdbNullIntAsZero))]
        public int Id { get; set; }
    }
}