using Newtonsoft.Json;

namespace TMDbLib.Objects.Lists
{
    internal class ListCreateReply
    {
        [JsonProperty("list_id")]
        public string ListId { get; set; }

        [JsonProperty("status_code")]
        public int StatusCode { get; set; }

        [JsonProperty("status_message")]
        public string StatusMessage { get; set; }

        [JsonProperty("success")]
        public bool Success { get; set; }
    }
}