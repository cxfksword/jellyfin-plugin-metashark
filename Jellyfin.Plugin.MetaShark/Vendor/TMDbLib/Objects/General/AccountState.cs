using Newtonsoft.Json;

namespace TMDbLib.Objects.General
{
    public class AccountState
    {
        /// <summary>
        /// Represents the current favorite status of the related movie for the current user session.
        /// </summary>
        [JsonProperty("favorite")]
        public bool Favorite { get; set; }

        /// <summary>
        /// The TMDb id for the related movie
        /// </summary>
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("rating")]
        public double? Rating { get; set; }

        /// <summary>
        /// Represents the presence of the related movie on the current user's watchlist.
        /// </summary>
        [JsonProperty("watchlist")]
        public bool Watchlist { get; set; }
    }
}
