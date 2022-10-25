using Newtonsoft.Json;

namespace TMDbLib.Objects.General
{
    public class ExternalIds
    {
        [JsonProperty("freebase_id")]
        public string FreebaseId { get; set; }

        [JsonProperty("freebase_mid")]
        public string FreebaseMid { get; set; }

        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("tvrage_id")]
        public string TvrageId { get; set; }
    }
}
