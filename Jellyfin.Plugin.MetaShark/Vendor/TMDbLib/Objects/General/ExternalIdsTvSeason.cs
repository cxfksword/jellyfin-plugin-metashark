using Newtonsoft.Json;

namespace TMDbLib.Objects.General
{
    public class ExternalIdsTvSeason : ExternalIds
    {
        [JsonProperty("tvdb_id")]
        public string TvdbId { get; set; }
    }
}