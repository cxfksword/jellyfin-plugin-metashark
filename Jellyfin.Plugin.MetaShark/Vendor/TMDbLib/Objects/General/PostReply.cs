using Newtonsoft.Json;

namespace TMDbLib.Objects.General
{
    internal class PostReply
    {
        [JsonProperty("status_code")]
        public int StatusCode { get; set; }

        [JsonProperty("status_message")]
        public string StatusMessage { get; set; }
    }
}
