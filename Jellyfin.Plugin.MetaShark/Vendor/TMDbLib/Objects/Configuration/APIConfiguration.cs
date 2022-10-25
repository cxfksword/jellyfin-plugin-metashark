using Newtonsoft.Json;
using System.Collections.Generic;

namespace TMDbLib.Objects.Configuration
{
    public class APIConfiguration
    {
        [JsonProperty("images")]
        public APIConfigurationImages Images { get; set; }

        [JsonProperty("change_keys")]
        public List<string> ChangeKeys { get; set; }
    }
}
