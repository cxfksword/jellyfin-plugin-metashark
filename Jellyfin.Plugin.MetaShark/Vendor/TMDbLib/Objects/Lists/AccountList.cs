using Newtonsoft.Json;
using TMDbLib.Objects.General;

namespace TMDbLib.Objects.Lists
{
    public class AccountList : TMDbList<int>
    {
        [JsonProperty("list_type")]
        public MediaType ListType { get; set; }
    }
}