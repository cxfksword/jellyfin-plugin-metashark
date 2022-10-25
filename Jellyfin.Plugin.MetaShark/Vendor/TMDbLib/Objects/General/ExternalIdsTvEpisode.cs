using Newtonsoft.Json;

namespace TMDbLib.Objects.General
{
    public class ExternalIdsTvEpisode : ExternalIds
    {
        [JsonProperty("imdb_id")]
        public string ImdbId { get; set; }

        [JsonProperty("tvdb_id")]
        public string TvdbId { get; set; }
    }
}