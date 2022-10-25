using System.Collections.Generic;
using Newtonsoft.Json;
using TMDbLib.Objects.General;

namespace TMDbLib.Objects.Search
{
    public class SearchPerson : SearchBase
    {
        public SearchPerson()
        {
            MediaType = MediaType.Person;
        }

        [JsonProperty("adult")]
        public bool Adult { get; set; }

        [JsonProperty("known_for")]
        public List<KnownForBase> KnownFor { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("profile_path")]
        public string ProfilePath { get; set; }
    }
}