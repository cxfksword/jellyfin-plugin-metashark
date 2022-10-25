using System.Collections.Generic;
using Newtonsoft.Json;
using TMDbLib.Objects.General;

namespace TMDbLib.Objects.TvShows
{
    public class TvEpisode : TvEpisodeBase
    {
        [JsonProperty("account_states")]
        public TvAccountState AccountStates { get; set; }

        [JsonProperty("credits")]
        public CreditsWithGuestStars Credits { get; set; }

        [JsonProperty("crew")]
        public List<Crew> Crew { get; set; }

        [JsonProperty("external_ids")]
        public ExternalIdsTvEpisode ExternalIds { get; set; }

        [JsonProperty("guest_stars")]
        public List<Cast> GuestStars { get; set; }

        [JsonProperty("images")]
        public StillImages Images { get; set; }

        [JsonProperty("videos")]
        public ResultContainer<Video> Videos { get; set; }
    }
}
