using Newtonsoft.Json;
using TMDbLib.Objects.Search;
using TMDbLib.Objects.People;

namespace TMDbLib.Objects.Find
{
    public class FindPerson : SearchPerson
    {
        [JsonProperty("gender")]
        public PersonGender Gender { get; set; }

        [JsonProperty("known_for_department")]
        public string KnownForDepartment { get; set; }
    }
}