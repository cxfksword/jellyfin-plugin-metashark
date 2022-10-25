using Newtonsoft.Json;

namespace TMDbLib.Objects.General
{
    public class ImagesWithId : Images
    {
        [JsonProperty("id")]
        public int Id { get; set; }
    }
}