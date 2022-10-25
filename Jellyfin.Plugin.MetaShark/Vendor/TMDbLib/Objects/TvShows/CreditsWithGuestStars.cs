using System.Collections.Generic;
using Newtonsoft.Json;

namespace TMDbLib.Objects.TvShows
{
    public class CreditsWithGuestStars : Credits
    {
        [JsonProperty("guest_stars")]
        public List<Cast> GuestStars { get; set; }
    }
}