using Newtonsoft.Json;

namespace TMDbLib.Objects.TvShows
{
    public class Network : NetworkBase
    {
        [JsonProperty("headquarters")]
        public string Headquarters { get; set; }

        [JsonProperty("homepage")]
        public string Homepage { get; set; }
    }
}