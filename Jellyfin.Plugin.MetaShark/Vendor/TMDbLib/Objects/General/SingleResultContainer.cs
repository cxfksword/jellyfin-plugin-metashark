using Newtonsoft.Json;

namespace TMDbLib.Objects.General
{
    public class SingleResultContainer<T>
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("results")]
        public T Results { get; set; }
    }
}