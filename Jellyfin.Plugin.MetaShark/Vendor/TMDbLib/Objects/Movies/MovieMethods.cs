using System;
using TMDbLib.Utilities;

namespace TMDbLib.Objects.Movies
{
    [Flags]
    public enum MovieMethods
    {
        [EnumValue("Undefined")]
        Undefined = 0,
        [EnumValue("alternative_titles")]
        AlternativeTitles = 1 << 0,
        [EnumValue("credits")]
        Credits = 1 << 1,
        [EnumValue("images")]
        Images = 1 << 2,
        [EnumValue("keywords")]
        Keywords = 1 << 3,
        [EnumValue("releases")]
        Releases = 1 << 4,
        [EnumValue("videos")]
        Videos = 1 << 5,
        [EnumValue("translations")]
        Translations = 1 << 6,
        [EnumValue("similar")]
        Similar = 1 << 7,
        [EnumValue("reviews")]
        Reviews = 1 << 8,
        [EnumValue("lists")]
        Lists = 1 << 9,
        [EnumValue("changes")]
        Changes = 1 << 10,
        /// <summary>
        /// Requires a valid user session to be set on the client object
        /// </summary>
        [EnumValue("account_states")]
        AccountStates = 1 << 11,
        [EnumValue("release_dates")]
        ReleaseDates = 1 << 12,
        [EnumValue("recommendations")]
        Recommendations = 1 << 13,
        [EnumValue("external_ids")]
        ExternalIds = 1 << 14,
        [EnumValue("watch/providers")]
        WatchProviders = 1 << 15
    }
}
