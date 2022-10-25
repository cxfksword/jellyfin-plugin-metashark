using System.Collections.Generic;
using Newtonsoft.Json;
using TMDbLib.Objects.General;

namespace TMDbLib.Objects.Genres
{
    public class GenreContainer
    {
        [JsonProperty("genres")]
        public List<Genre> Genres { get; set; }
    }
}