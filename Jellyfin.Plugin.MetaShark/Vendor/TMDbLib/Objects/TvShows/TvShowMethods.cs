using System;
using TMDbLib.Utilities;

namespace TMDbLib.Objects.TvShows
{
    [Flags]
    public enum TvShowMethods
    {
        [EnumValue("Undefined")]
        Undefined = 0,
        [EnumValue("credits")]
        Credits = 1 << 0,
        [EnumValue("images")]
        Images = 1 << 1,
        [EnumValue("external_ids")]
        ExternalIds = 1 << 2,
        [EnumValue("content_ratings")]
        ContentRatings = 1 << 3,
        [EnumValue("alternative_titles")]
        AlternativeTitles = 1 << 4,
        [EnumValue("keywords")]
        Keywords = 1 << 5,
        [EnumValue("similar")]
        Similar = 1 << 6,
        [EnumValue("videos")]
        Videos = 1 << 7,
        [EnumValue("translations")]
        Translations = 1 << 8,
        [EnumValue("account_states")]
        AccountStates = 1 << 9,
        [EnumValue("changes")]
        Changes = 1 << 10,
        [EnumValue("recommendations")]
        Recommendations = 1 << 11,
        [EnumValue("reviews")]
        Reviews = 1 << 12,
        [EnumValue("watch/providers")]
        WatchProviders = 1 << 13,
        [EnumValue("episode_groups")]
        EpisodeGroups = 1 << 14,
        [EnumValue("aggregate_credits")]
        CreditsAggregate = 1 << 15,
    }
}
