using Newtonsoft.Json;

namespace TMDbLib.Objects.Reviews
{
    public class AuthorDetails
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("username")]
        public string Username { get; set; }

        [JsonProperty("avatar_path")]
        public string AvatarPath { get; set; }

        [JsonProperty("rating")]
        public string Rating { get; set; }
    }
}