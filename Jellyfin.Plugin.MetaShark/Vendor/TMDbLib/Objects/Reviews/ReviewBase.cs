using Newtonsoft.Json;
using TMDbLib.Objects.Reviews;

namespace TMDbLib.Objects.Reviews
{
    public class ReviewBase
    {
        [JsonProperty("author")]
        public string Author { get; set; }

        [JsonProperty("author_details")]
        public AuthorDetails AuthorDetails { get; set; }

        [JsonProperty("content")]
        public string Content { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }
    }
}