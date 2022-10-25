using Newtonsoft.Json;

namespace TMDbLib.Objects.TvShows
{
    public class NetworkWithLogo : NetworkBase
    {
        [JsonProperty("logo_path")]
        public string LogoPath { get; set; }
    }
}
