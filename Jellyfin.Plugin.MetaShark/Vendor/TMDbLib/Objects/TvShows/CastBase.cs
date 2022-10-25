using Newtonsoft.Json;
using TMDbLib.Objects.People;

namespace TMDbLib.Objects.TvShows
{
    public class CastBase
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("order")]
        public int Order { get; set; }

        [JsonProperty("profile_path")]
        public string ProfilePath { get; set; }

        [JsonProperty("gender")]
        public PersonGender Gender { get; set; }

        [JsonProperty("adult")]
        public bool Adult { get; set; }

        [JsonProperty("known_for_department")]
        public string KnownForDepartment { get; set; }

        [JsonProperty("original_name")]
        public string OriginalName { get; set; }

        [JsonProperty("popularity")]
        public float Popularity { get; set; }
    }
}