using TMDbLib.Utilities;

namespace TMDbLib.Objects.Discover
{
    public enum DiscoverMovieSortBy
    {
        Undefined,
        [EnumValue("popularity.asc")]
        Popularity,
        [EnumValue("popularity.desc")]
        PopularityDesc,
        [EnumValue("release_date.asc")]
        ReleaseDate,
        [EnumValue("release_date.desc")]
        ReleaseDateDesc,
        [EnumValue("revenue.asc")]
        Revenue,
        [EnumValue("revenue.desc")]
        RevenueDesc,
        [EnumValue("primary_release_date.asc")]
        PrimaryReleaseDate,
        [EnumValue("primary_release_date.desc")]
        PrimaryReleaseDateDesc,
        [EnumValue("original_title.asc")]
        OriginalTitle,
        [EnumValue("original_title.desc")]
        OriginalTitleDesc,
        [EnumValue("vote_average.asc")]
        VoteAverage,
        [EnumValue("vote_average.desc")]
        VoteAverageDesc,
        [EnumValue("vote_count.asc")]
        VoteCount,
        [EnumValue("vote_count.desc")]
        VoteCountDesc
    }
}
