using System.Collections.Generic;
using Newtonsoft.Json;
using TMDbLib.Objects.Search;

namespace TMDbLib.Objects.Lists
{
    public class GenericList : TMDbList<string>
    {
        [JsonProperty("created_by")]
        public string CreatedBy { get; set; }

        [JsonProperty("items")]
        public List<SearchBase> Items { get; set; }
    }
}