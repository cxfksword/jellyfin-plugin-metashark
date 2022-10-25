using Newtonsoft.Json;
using TMDbLib.Objects.General;

namespace TMDbLib.Objects.Reviews
{
    public class Review : ReviewBase
    {
        /// <summary>
        /// A language code, e.g. en
        /// </summary>
        [JsonProperty("iso_639_1")]
        public string Iso_639_1 { get; set; }

        [JsonProperty("media_id")]
        public int MediaId { get; set; }

        [JsonProperty("media_title")]
        public string MediaTitle { get; set; }

        [JsonProperty("media_type")]
        public MediaType MediaType { get; set; }
    }
}
