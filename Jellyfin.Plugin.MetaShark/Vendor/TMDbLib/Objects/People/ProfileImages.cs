using System.Collections.Generic;
using Newtonsoft.Json;
using TMDbLib.Objects.General;

namespace TMDbLib.Objects.People
{
    public class ProfileImages
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("profiles")]
        public List<ImageData> Profiles { get; set; }
    }
}