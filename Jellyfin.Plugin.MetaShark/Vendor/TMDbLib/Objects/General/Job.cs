using System.Collections.Generic;
using Newtonsoft.Json;

namespace TMDbLib.Objects.General
{
    public class Job
    {
        [JsonProperty("department")]
        public string Department { get; set; }

        [JsonProperty("jobs")]
        public List<string> Jobs { get; set; }
    }
}
