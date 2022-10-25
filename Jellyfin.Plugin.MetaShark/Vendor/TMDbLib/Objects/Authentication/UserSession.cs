using Newtonsoft.Json;

namespace TMDbLib.Objects.Authentication
{
    /// <summary>
    /// Session object that can be retrieved after the user has correctly authenticated himself on the TMDb site. (using the referal url from the token provided previously)
    /// </summary>
    public class UserSession
    {
        [JsonProperty("session_id")]
        public string SessionId { get; set; }

        [JsonProperty("success")]
        public bool Success { get; set; }
    }
}
